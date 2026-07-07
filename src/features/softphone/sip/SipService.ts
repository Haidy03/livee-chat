/**
 * SipService: thin wrapper around JsSIP.UA, exposing a SipAdapter interface.
 *
 * - Single active session at a time (transfer is handled via REFER)
 * - Auto-reconnect with exponential backoff is enabled via JsSIP options
 * - Emits "registration" / "call" / "debug" events on a shared bus
 *
 * NOTE: We never log the password and we reject ws:// URLs — only WSS allowed.
 */

import JsSIP from "jssip";
import { SipEventBus } from "./eventBus";
import { withTenantExtension } from "./formatSipTarget";
import type {
  ActiveSipCall,
  CallEvent,
  CallRecordEvent,
  RegistrationEvent,
  SipAdapter,
  SipConfig,
  SipDebugEvent,
  SipEventName,
  SipEventHandler,
} from "./types";

/** Sanitize a SIP Call-ID so it's safe as a filename. Must mirror Asterisk's FILTER. */
export function safeCallId(callId: string): string {
  return (callId || "").replace(/[^A-Za-z0-9_-]/g, "_");
}

/** Cached SIP domain from the last successful register(), reused by the spy controller. */
let lastRegisteredSipUri = "";
export function getRegisteredSipDomain(): string {
  const uri = lastRegisteredSipUri;
  if (!uri) return "";
  const at = uri.indexOf("@");
  if (at < 0) return "";
  return uri.slice(at + 1).replace(/[;>].*$/, "");
}

// Silence JsSIP's verbose logger; we surface our own debug events.
JsSIP.debug.disable();

/** Extract the first SIP start-line for compact tracing (e.g. "REGISTER sip:host SIP/2.0" or "SIP/2.0 401 Unauthorized"). */
function sipStartLine(raw: string): string {
  const line = (raw || "").split(/\r?\n/, 1)[0] || "";
  return line.length > 160 ? line.slice(0, 160) + "…" : line;
}

type RTCSession = ReturnType<JsSIP.UA["call"]> | unknown;

function loadSavedSpeakerId(): string {
  try {
    const raw = localStorage.getItem("softphone:audio-prefs");
    if (!raw) return "";
    const parsed = JSON.parse(raw) as { speakerId?: string };
    return typeof parsed.speakerId === "string" ? parsed.speakerId : "";
  } catch {
    return "";
  }
}

interface JsSIPSession {
  id: string;
  direction: "incoming" | "outgoing";
  remote_identity: { uri: { user: string; toString: () => string }; display_name?: string };
  local_identity: { uri: { user: string; toString: () => string }; display_name?: string };
  request: { call_id: string };
  connection: RTCPeerConnection;
  on(event: string, handler: (data?: unknown) => void): void;
  answer(opts?: unknown): void;
  terminate(opts?: unknown): void;
  isEstablished(): boolean;
  isInProgress(): boolean;
  isEnded(): boolean;
  mute(opts: { audio: boolean }): void;
  unmute(opts: { audio: boolean }): void;
  hold(): boolean;
  unhold(): boolean;
  sendDTMF(tone: string): void;
  refer(target: string, opts?: unknown): void;
}

export class SipService implements SipAdapter {
  private bus = new SipEventBus();
  private ua: JsSIP.UA | null = null;
  private session: JsSIPSession | null = null;
  private active: ActiveSipCall | null = null;
  private localStream: MediaStream | null = null;
  private remoteStream: MediaStream | null = null;
  private remoteAudio: HTMLAudioElement | null = null;
  private currentSinkId = "";
  private localReject = false;
  private answering = false;
  private holdStartedAt: number | null = null;
  private accumulatedHoldMs = 0;
  private endedAt: number | null = null;
  private authId = "";
  private sessionHooks: {
    markAnswerSent: () => void;
    promoteAfterAnswer: () => void;
  } | null = null;

  on(event: SipEventName, handler: SipEventHandler): () => void {
    return this.bus.on(event, handler);
  }

  private debug(level: SipDebugEvent["level"], message: string, detail?: unknown) {
    const evt: SipDebugEvent = { at: Date.now(), level, message, detail };
    this.bus.emit("debug", evt);
  }

  private emitRegistration(status: RegistrationEvent["status"], reason?: string) {
    this.bus.emit("registration", { status, reason } satisfies RegistrationEvent);
  }

  private emitCall() {
    this.bus.emit("call", { call: this.active } satisfies CallEvent);
  }

  private emitCallRecord(callId: string, patch: CallRecordEvent["patch"]) {
    if (!callId) return;
    this.bus.emit("callRecord", { callId, patch } satisfies CallRecordEvent);
  }

  /** Compute duration fields from authoritative timestamps. Always non-negative seconds. */
  private computeDurations(): {
    ring_seconds: number;
    hold_seconds: number;
    active_seconds: number;
    total_hold_seconds: number;
    total_seconds: number;
  } {
    const a = this.active;
    if (!a) {
      return { ring_seconds: 0, hold_seconds: 0, active_seconds: 0, total_hold_seconds: 0, total_seconds: 0 };
    }
    const nowOrEnded = this.endedAt ?? Date.now();
    const startedAt = a.startedAt;
    const connectedAt = a.connectedAt;
    const currentHoldMs = this.holdStartedAt ? Math.max(0, nowOrEnded - this.holdStartedAt) : 0;
    const totalHoldMs = this.accumulatedHoldMs + currentHoldMs;
    const ringMs = Math.max(0, (connectedAt ?? nowOrEnded) - startedAt);
    const activeMs = connectedAt
      ? Math.max(0, nowOrEnded - connectedAt - totalHoldMs)
      : 0;
    const totalMs = Math.max(0, nowOrEnded - startedAt);
    const s = (ms: number) => Math.floor(ms / 1000);
    // Ring uses round so a sub-second answer (e.g. 780ms) is recorded as 1s
    // instead of being lost as 0. Also guarantee ≥1s when the call was actually
    // answered so the breakdown row + active + ring + hold = total still holds.
    const ringSeconds = connectedAt
      ? Math.max(1, Math.round(ringMs / 1000))
      : s(ringMs);
    return {
      ring_seconds: ringSeconds,
      hold_seconds: s(currentHoldMs),
      active_seconds: s(activeMs),
      total_hold_seconds: s(totalHoldMs),
      total_seconds: s(totalMs),
    };
  }

  private ensureRemoteAudio(): HTMLAudioElement {
    if (!this.remoteAudio) {
      const el = document.createElement("audio");
      el.autoplay = true;
      (el as HTMLAudioElement & { playsInline?: boolean }).playsInline = true;
      el.controls = false;
      el.setAttribute("data-softphone-remote", "1");
      el.style.display = "none";
      try { document.body.appendChild(el); } catch { /* ignore */ }
      this.remoteAudio = el;
      if (!this.currentSinkId) {
        const saved = loadSavedSpeakerId();
        if (saved) this.currentSinkId = saved;
      }
    }
    return this.remoteAudio;
  }

  private async playRemoteAudio() {
    const el = this.ensureRemoteAudio();
    try {
      await el.play();
    } catch (err) {
      this.debug("warn", "Remote audio autoplay blocked", err);
      try {
        const { toast } = await import("sonner");
        toast.message("Click anywhere to enable call audio");
      } catch { /* ignore */ }
      const retry = () => {
        el.play().catch(() => {});
        document.removeEventListener("click", retry);
        document.removeEventListener("keydown", retry);
        document.removeEventListener("touchstart", retry);
      };
      document.addEventListener("click", retry, { once: true });
      document.addEventListener("keydown", retry, { once: true });
      document.addEventListener("touchstart", retry, { once: true });
    }
  }

  async setSinkId(sinkId: string) {
    this.currentSinkId = sinkId;
    const el = this.ensureRemoteAudio() as HTMLAudioElement & { setSinkId?: (id: string) => Promise<void> };
    if (typeof el.setSinkId === "function") {
      try {
        await el.setSinkId(sinkId);
      } catch (err) {
        this.debug("warn", "setSinkId failed", err);
      }
    }
  }

  /**
   * Pre-create the remote <audio> element and unlock autoplay by playing
   * a brief muted no-op. Must be called synchronously from a user-gesture
   * handler (e.g. the Accept-call click) so Safari/iOS allow later play().
   */
  prewarmAudio(): void {
    const el = this.ensureRemoteAudio();
    try {
      // Attach a tiny silent MediaStream so Safari/Firefox actually unlock
      // autoplay (an empty audio element with no source won't unlock).
      try {
        const AC = (window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext);
        if (AC && !el.srcObject) {
          const ctx = new AC();
          const dst = ctx.createMediaStreamDestination();
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          gain.gain.value = 0;
          osc.connect(gain).connect(dst);
          osc.start();
          el.srcObject = dst.stream;
          window.setTimeout(() => { try { osc.stop(); ctx.close(); } catch { /* ignore */ } }, 250);
        }
      } catch { /* ignore */ }
      const wasMuted = el.muted;
      el.muted = true;
      const p = el.play();
      if (p && typeof (p as Promise<void>).catch === "function") {
        (p as Promise<void>).catch(() => {});
      }
      window.setTimeout(() => { try { el.muted = wasMuted; } catch { /* ignore */ } }, 0);
    } catch { /* ignore */ }
  }

  async register(cfg: SipConfig): Promise<void> {
    if (!cfg.wsUrl.startsWith("wss://")) {
      const reason = "Only wss:// (secure WebSocket) URLs are allowed.";
      this.emitRegistration("failed", reason);
      throw new Error(reason);
    }

    await this.unregister();

    this.authId = cfg.authId || "";
    lastRegisteredSipUri = cfg.sipUri || "";

    const socket = new JsSIP.WebSocketInterface(cfg.wsUrl);

    // Wrap socket send/receive so we can trace REGISTER traffic. JsSIP exposes
    // `send(data)` directly and assigns to `ondata` later (in Transport.connect).
    // We use a property accessor so any future assignment to `ondata` is captured.
    const sock = socket as unknown as {
      send: (d: string) => unknown;
      ondata?: (d: string) => void;
    };
    const originalSend = sock.send.bind(sock);
    sock.send = (data: string) => {
      try { this.debug("info", "SIP → " + sipStartLine(String(data))); } catch { /* ignore */ }
      return originalSend(data);
    };

    let realOnData: ((d: string) => void) | undefined;
    const traceInbound = (data: string) => {
      try { this.debug("info", "SIP ← " + sipStartLine(String(data))); } catch { /* ignore */ }
      if (typeof realOnData === "function") {
        try { realOnData(data); } catch (err) { this.debug("error", "ondata handler threw", err); }
      }
    };
    try {
      Object.defineProperty(sock, "ondata", {
        configurable: true,
        enumerable: true,
        get: () => traceInbound,
        set: (fn) => { realOnData = fn; },
      });
      this.debug("info", "SIP wire trace installed");
    } catch (err) {
      this.debug("warn", "Could not install ondata trace", err);
    }

    const iceServers: RTCIceServer[] = [];
    cfg.stunUrls.filter(Boolean).forEach((u) => iceServers.push({ urls: u }));
    if (cfg.turnUrl) {
      iceServers.push({
        urls: cfg.turnUrl,
        username: cfg.turnUsername || undefined,
        credential: cfg.turnPassword || undefined,
      });
    }

    const ua = new JsSIP.UA({
      sockets: [socket],
      uri: cfg.sipUri,
      password: cfg.password,
      authorization_user: cfg.authId || undefined,
      display_name: cfg.displayName || undefined,
      register: true,
      session_timers: true,
      // Built-in reconnect (exponential-ish) on socket loss
      connection_recovery_min_interval: 2,
      connection_recovery_max_interval: 30,
    });

    ua.on("connecting", () => { this.debug("info", "UA connecting…"); this.emitRegistration("connecting"); });
    ua.on("connected", () => this.debug("info", "WebSocket connected"));
    ua.on("disconnected", (e) => {
      this.emitRegistration("connecting");
      this.debug("warn", "WebSocket disconnected", e);
    });
    ua.on("registered", () => this.emitRegistration("registered"));
    ua.on("unregistered", () => this.emitRegistration("unregistered"));
    ua.on("registrationFailed", (e) =>
      this.emitRegistration("failed", (e as { cause?: string })?.cause || "Registration failed"),
    );

    ua.on("newRTCSession", (data: unknown) => {
      const d = data as { session: JsSIPSession; originator: "local" | "remote" };
      this.attachSession(d.session, d.originator === "remote" ? "in" : "out");
    });

    this.ua = ua;
    this.iceServers = iceServers;
    this.emitRegistration("connecting");
    this.debug("info", `Calling ua.start() for ${cfg.sipUri} via ${cfg.wsUrl}`);
    try {
      ua.start();
      this.debug("info", "ua.start() returned");
    } catch (err) {
      this.debug("error", "ua.start() threw", err);
      this.emitRegistration("failed", (err as Error)?.message || "ua.start() failed");
      throw err;
    }
  }

  private iceServers: RTCIceServer[] = [];

  async unregister(): Promise<void> {
    if (this.session) {
      try {
        this.session.terminate();
      } catch {
        /* ignore */
      }
      this.session = null;
      this.answering = false;
    }
    if (this.ua) {
      try {
        this.ua.stop();
      } catch {
        /* ignore */
      }
      this.ua = null;
    }
    this.active = null;
    this.emitCall();
    this.emitRegistration("unregistered");
  }

  private attachSession(session: JsSIPSession, direction: "in" | "out") {
    if (this.session) {
      // Reject a second concurrent call
      try {
        session.terminate({ status_code: 486, reason_phrase: "Busy Here" });
      } catch {
        /* ignore */
      }
      return;
    }
    this.session = session;
    this.localReject = false;
    this.holdStartedAt = null;
    this.accumulatedHoldMs = 0;
    this.endedAt = null;

    // Short-circuit ICE gathering so JsSIP sends INVITE/200-OK quickly instead
    // of waiting up to 40s for full gathering on PCs with many NICs.
    // Strategy: arm a 1.2s timeout on the first local candidate; whichever
    // happens first (timeout fires or natural gathering finishes) we call
    // ready() to release the SDP. Remote will negotiate against what we have.
    const iceStartedAt = Date.now();
    let iceReadyCalled = false;
    let iceTimer: number | null = null;
    const ICE_BUDGET_MS = 1200;
    (session as unknown as { on: (e: string, h: (d: { candidate: RTCIceCandidate | null; ready: () => void }) => void) => void }).on(
      "icecandidate",
      ({ candidate, ready }) => {
        if (iceReadyCalled) return;
        const dt = Date.now() - iceStartedAt;
        if (candidate) {
          this.debug("info", `[ICE] gathered candidate: ${candidate.type ?? "?"}`, { dtMs: dt });
        }
        if (iceTimer == null) {
          iceTimer = window.setTimeout(() => {
            if (iceReadyCalled) return;
            iceReadyCalled = true;
            this.debug("info", "[ICE] ready() — releasing INVITE (timeout)", { dtMs: Date.now() - iceStartedAt });
            try { ready(); } catch (err) { this.debug("warn", "ice ready() threw", err); }
          }, ICE_BUDGET_MS);
        }
      },
    );



    const remoteUser = session.remote_identity?.uri?.user ?? "";
    const remoteDisplayName = session.remote_identity?.display_name ?? "";
    const req = session.request as unknown as {
      call_id?: string;
      from_tag?: string;
      getHeader?: (name: string) => string | undefined;
      headers?: Record<string, Array<{ raw?: string }>>;
    } | undefined;
    // JsSIP stores the actual outbound INVITE on RTCSession._request and the
    // local from-tag on RTCSession._from_tag. For outgoing calls, session.request
    // is not yet populated at newRTCSession time, so we must read the private
    // _request / _from_tag fields to get the real wire Call-ID.
    const sessionPrivate = session as unknown as {
      _request?: { call_id?: string };
      _from_tag?: string;
    };
    const sessionId = session.id ?? "";
    const requestCallId = req?.call_id ?? "";
    const privateRequestCallId = sessionPrivate._request?.call_id ?? "";
    const headerCallId = (() => {
      try { return req?.getHeader?.("Call-ID") ?? ""; } catch { return ""; }
    })();
    const rawHeaderCallId = req?.headers?.["Call-ID"]?.[0]?.raw ?? "";
    const privateFromTag = sessionPrivate._from_tag ?? "";
    const fromTag =
      privateFromTag ||
      req?.from_tag ||
      (session as unknown as { from_tag?: string }).from_tag ||
      "";
    const derivedFromSessionId = fromTag && sessionId.endsWith(fromTag)
      ? sessionId.slice(0, sessionId.length - fromTag.length)
      : "";
    const callId =
      privateRequestCallId ||
      requestCallId ||
      headerCallId ||
      rawHeaderCallId ||
      derivedFromSessionId ||
      sessionId;
    const callIdTrace = {
      direction,
      privateRequestCallId,
      requestCallId,
      headerCallId,
      rawHeaderCallId,
      derivedFromSessionId,
      privateFromTag,
      fromTag,
      sessionId,
      resolvedCallId: callId,
      remoteUri: session.remote_identity?.uri?.toString?.() ?? "",
      localUri: session.local_identity?.uri?.toString?.() ?? "",
    };
    this.debug("info", `[CallID] ${direction.toUpperCase()} INVITE attach`, callIdTrace);
    console.info("[SIP][CallID] attach", callIdTrace);

    const fromUri = direction === "in"
      ? session.remote_identity?.uri?.toString?.() ?? ""
      : session.local_identity?.uri?.toString?.() ?? "";
    const fromDisplay = direction === "in"
      ? session.remote_identity?.display_name ?? null
      : session.local_identity?.display_name ?? null;
    const toUri = direction === "in"
      ? session.local_identity?.uri?.toString?.() ?? ""
      : session.remote_identity?.uri?.toString?.() ?? "";
    const toDisplay = direction === "in"
      ? session.local_identity?.display_name ?? null
      : session.remote_identity?.display_name ?? null;

    this.active = {
      id: session.id,
      callId,
      remoteNumber: remoteUser,
      remoteDisplayName,
      fromUri,
      direction,
      startedAt: Date.now(),
      connectedAt: null,
      phase: direction === "in" ? "inbound_ringing" : "outbound_dialing",
      muted: false,
      onHold: false,
    };
    this.emitCall();

    // Persist initial "ringing" row keyed by Call-ID.
    this.emitCallRecord(callId, {
      status: "ringing",
      direction,
      from_uri: fromUri,
      from_display: fromDisplay,
      to_uri: toUri,
      to_display: toDisplay,
      started_at: new Date().toISOString(),
      ...(direction === "out"
        ? { caller: this.authId }
        : { called: this.authId }),
    });

    session.on("progress", () => {
      if (this.active && this.active.phase === "outbound_dialing") {
        this.active = { ...this.active, phase: "outbound_ringing" };
        this.emitCall();
      }
    });

    let answered = false;
    let answerSent = false;
    const promoteToActive = (reason: string) => {
      if (!this.active) return;
      if (this.active.phase === "active") return;
      answered = true;
      this.answering = false;
      this.active = { ...this.active, phase: "active", connectedAt: this.active.connectedAt ?? Date.now() };
      this.emitCall();
      this.emitCallRecord(callId, {
        status: "in-progress",
        answered_at: new Date().toISOString(),
        ...this.computeDurations(),
      });
      this.debug("info", `Call promoted to active (${reason})`);
    };
    this.sessionHooks = {
      markAnswerSent: () => { answerSent = true; },
      promoteAfterAnswer: () => {
        if (!this.active) return;
        if (this.active.phase !== "inbound_ringing") return;
        if (!this.remoteStream) return;
        promoteToActive("post-answer");
      },
    };
    session.on("accepted", () => promoteToActive("accepted"));
    session.on("confirmed", () => {
      promoteToActive("confirmed");
      // Note: do NOT emit a durations-only callRecord here. The promoteToActive
      // above already emits an "in-progress" upsert on first transition; a
      // second emit with only durations would generate an extra POST per call.
    });

    session.on("hold", () => {
      if (this.holdStartedAt == null) this.holdStartedAt = Date.now();
      // No upsert on hold — final hold seconds are included in the terminal
      // ended/failed emit via computeDurations().
    });
    session.on("unhold", () => {
      if (this.holdStartedAt != null) {
        this.accumulatedHoldMs += Math.max(0, Date.now() - this.holdStartedAt);
        this.holdStartedAt = null;
      }
      // No upsert on unhold — see hold handler above.
    });

    session.on("ended", () => {
      this.endedAt = Date.now();
      if (this.holdStartedAt != null) {
        this.accumulatedHoldMs += Math.max(0, this.endedAt - this.holdStartedAt);
        this.holdStartedAt = null;
      }
      const safe = safeCallId(callId);
      console.info("[SIP][CallID] terminal", { phase: "ended", callId, sessionId, requestCallId });
      this.emitCallRecord(callId, {
        status: "completed",
        ended_at: new Date(this.endedAt).toISOString(),
        hangup_cause: "Terminated",
        recording_url: `/api/recordings/${safe}.wav`,
        has_recording: true,
        ...this.computeDurations(),
      });
      this.cleanupSession("ended");
    });
    session.on("failed", (data) => {
      this.endedAt = Date.now();
      if (this.holdStartedAt != null) {
        this.accumulatedHoldMs += Math.max(0, this.endedAt - this.holdStartedAt);
        this.holdStartedAt = null;
      }
      const cause = (data as { cause?: string })?.cause || "Call failed";
      console.info("[SIP][CallID] terminal", { phase: "failed", callId, sessionId, requestCallId, cause });
      let status: "rejected" | "missed" | "failed" | "busy" = "failed";
      let friendlyReason = cause;
      if (this.localReject) status = "rejected";
      else if (/busy/i.test(cause)) status = "busy";
      else if (!answered && direction === "in") status = "missed";
      else if (!answered && (cause === "Rejected" || cause === "Canceled")) status = "rejected";
      if (!answered && direction === "out" && /reject|unavailable|timeout|cancel|decline/i.test(cause)) {
        friendlyReason = "Call was not answered or was rejected by server";
      }
      this.emitCallRecord(callId, {
        status,
        ended_at: new Date(this.endedAt).toISOString(),
        hangup_cause: cause,
        ...this.computeDurations(),
      });
      this.cleanupSession("failed", friendlyReason);
    });

    // Wire up audio: JsSIP creates the RTCPeerConnection only after
    // answer()/call() runs, so session.connection is usually null here.
    // Listen for the "peerconnection" event to attach the track listener
    // as soon as the PC actually exists.
    const wirePc = (pc: RTCPeerConnection) => {

      // ICE timeline: track gathering states + first candidate per type.
      const pcStart = Date.now();
      this.debug("info", `[ICE] gathering: ${pc.iceGatheringState}`, { dtMs: 0 });
      const seenTypes = new Set<string>();
      pc.addEventListener("icegatheringstatechange", () => {
        this.debug("info", `[ICE] gathering: ${pc.iceGatheringState}`, { dtMs: Date.now() - pcStart });
      });
      pc.addEventListener("icecandidate", (e) => {
        const c = e.candidate;
        if (!c) {
          this.debug("info", "[ICE] candidate: end-of-candidates", { dtMs: Date.now() - pcStart });
          return;
        }
        const type = c.type || "unknown";
        if (seenTypes.has(type)) return;
        seenTypes.add(type);
        this.debug("info", `[ICE] candidate: ${type}`, {
          dtMs: Date.now() - pcStart,
          protocol: c.protocol,
          address: c.address,
        });
      });
      pc.addEventListener("iceconnectionstatechange", () => {
        this.debug("info", `[ICE] connection: ${pc.iceConnectionState}`, { dtMs: Date.now() - pcStart });
      });
      pc.addEventListener("track", (e) => {
        const stream = e.streams[0] ?? new MediaStream([e.track]);
        this.remoteStream = stream;
        const el = this.ensureRemoteAudio();
        el.srcObject = stream;
        this.debug("info", "Remote audio track attached", { kind: e.track.kind, sinkId: this.currentSinkId });
        void this.playRemoteAudio();
        if (this.currentSinkId) this.setSinkId(this.currentSinkId).catch(() => {});
        // Promote to active on remote media:
        // - Outbound: any time during dialing/ringing (proves callee answered)
        // - Inbound: only after we've actually dispatched 200 OK via answer()
        if (
          this.active &&
          ((this.active.direction === "out" &&
            (this.active.phase === "outbound_ringing" || this.active.phase === "outbound_dialing")) ||
            (this.active.direction === "in" && answerSent && this.active.phase === "inbound_ringing"))
        ) {
          promoteToActive("remote-track");
        }
      });
      // Legacy fallback for older Chromium / some PBX paths
      try {
        pc.addEventListener("addstream" as keyof RTCPeerConnectionEventMap, ((ev: Event) => {
          const stream = (ev as Event & { stream?: MediaStream }).stream;
          if (!stream) return;
          this.remoteStream = stream;
          const el = this.ensureRemoteAudio();
          el.srcObject = stream;
          this.debug("info", "Remote audio addstream (legacy)", { sinkId: this.currentSinkId });
          void this.playRemoteAudio();
          if (this.currentSinkId) this.setSinkId(this.currentSinkId).catch(() => {});
          if (
            this.active &&
            ((this.active.direction === "out" &&
              (this.active.phase === "outbound_ringing" || this.active.phase === "outbound_dialing")) ||
              (this.active.direction === "in" && answerSent && this.active.phase === "inbound_ringing"))
          ) {
            promoteToActive("remote-addstream");
          }
        }) as EventListener);
      } catch { /* ignore */ }
    };

    session.on("peerconnection", (data: unknown) => {
      const pc = (data as { peerconnection?: RTCPeerConnection })?.peerconnection;
      if (pc) wirePc(pc);
    });

    if (session.connection) wirePc(session.connection);
  }

  private cleanupSession(phase: "ended" | "failed", reason?: string) {
    if (this.active) {
      this.active = { ...this.active, phase, failureReason: reason };
      this.emitCall();
    }
    this.stopMedia();
    this.session = null;
    this.sessionHooks = null;
    this.localReject = false;
    this.answering = false;
    // After a brief delay, clear the active call so UI returns to idle.
    window.setTimeout(() => {
      this.active = null;
      this.emitCall();
    }, 1200);
  }

  async call(target: string, displayName?: string): Promise<void> {
    if (!this.ua) throw new Error("Not registered. Configure your SIP account first.");
    if (this.session) throw new Error("Another call is already in progress.");

    const t0 = Date.now();
    this.debug("info", "[DIAL] click", { t0 });

    // Pre-create the <audio> element inside the user gesture so autoplay isn't blocked.
    this.ensureRemoteAudio();

    // Acquire mic up-front so we surface permission errors early.
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.debug("info", "[DIAL] mic acquired", { dtMs: Date.now() - t0 });
    } catch (err) {
      this.debug("error", "Microphone permission denied", err);
      throw new Error("Microphone access denied. Please grant permission and retry.");
    }

    const tenantTarget = withTenantExtension(target);
    const fullTarget = tenantTarget.includes("@")
      ? tenantTarget
      : `sip:${tenantTarget.replace(/[^\w+#*\-.]/g, "")}@${this.uaHost()}`;

    this.debug("info", `[INVITE] target=${fullTarget}`, { rawTarget: target, tenantTarget, fullTarget, displayName });
    console.info("[SIP][INVITE] target", { rawTarget: target, tenantTarget, fullTarget, displayName });

    this.ua.call(fullTarget, {
      mediaConstraints: { audio: true, video: false },
      mediaStream: this.localStream,
      // Speed: the icecandidate handler in attachSession() calls ready() after
      // ~1.2s so the INVITE is dispatched without waiting for full gathering.
      pcConfig: { iceServers: this.iceServers },
      extraHeaders: displayName ? [`X-Display-Name: ${displayName}`] : [],
    });
    this.debug("info", "[DIAL] ua.call() returned", { dtMs: Date.now() - t0 });
  }


  private uaHost(): string {
    // Reach into JsSIP UA configuration without a public typing
    const cfg = (this.ua as unknown as { configuration?: { uri?: { toString: () => string } } } | null)?.configuration;
    const uri = cfg?.uri?.toString?.() ?? "";
    return uri.split("@")[1] ?? "";
  }

  async answer(): Promise<void> {
    if (!this.session) { this.debug("warn", "answer() called with no session"); return; }
    if (this.answering) { this.debug("warn", "answer() ignored: already answering"); return; }
    const jsSipStatus = (this.session as unknown as { status?: number }).status;
    if (typeof jsSipStatus === "number" && jsSipStatus !== 4) {
      this.debug("warn", `answer() ignored: jsSIP status=${jsSipStatus} (expected 4)`);
      return;
    }
    this.answering = true;
    this.debug("info", "answer() invoked — acquiring mic");
    this.ensureRemoteAudio();
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (err) {
      this.answering = false;
      this.debug("error", "Microphone permission denied on answer", err);
      try { this.session.terminate({ status_code: 486 }); } catch { /* ignore */ }
      throw new Error("Microphone access denied. Please grant permission and retry.");
    }
    try {
      this.debug("info", "answer() sending 200 OK");
      this.session.answer({
        mediaConstraints: { audio: true, video: false },
        mediaStream: this.localStream,
        pcConfig: { iceServers: this.iceServers },
      });
      this.sessionHooks?.markAnswerSent();
      this.debug("info", "session.answer() dispatched");
      // Safety net: some JsSIP / PBX combinations don't fire "accepted" or
      // "confirmed" reliably after our 200 OK (especially when remote-audio
      // autoplay is blocked). If remote media has arrived but we're still in
      // inbound_ringing, force the call to active so the UI moves forward and
      // the IncomingCallModal safety timeout doesn't auto-decline.
      const hooks = this.sessionHooks;
      window.setTimeout(() => { hooks?.promoteAfterAnswer(); }, 1500);
      this.debug("info", "post-answer promotion armed");
    } catch (err) {
      this.answering = false;
      const msg = (err as Error)?.message ?? "";
      if (/Invalid status/i.test(msg)) {
        this.debug("warn", "answer() ignored: session not in waiting state", err);
        return;
      }
      this.debug("error", "session.answer() threw", err);
      throw err;
    }
  }

  async reject(): Promise<void> {
    if (!this.session) return;
    this.localReject = true;
    try {
      this.session.terminate({ status_code: 603, reason_phrase: "Decline" });
    } catch {
      /* ignore */
    }
    this.stopMedia();
  }

  async hangup(): Promise<void> {
    if (!this.session) return;
    try {
      this.session.terminate();
    } catch {
      /* ignore */
    }
    // Immediately stop remote audio playback so the user doesn't keep hearing
    // the far end while the BYE / final ACK is still in-flight (some PBXs
    // delay the 'ended' event by tens of seconds).
    this.stopMedia();
  }

  private stopMedia() {
    if (this.remoteAudio) {
      try { this.remoteAudio.pause(); } catch { /* ignore */ }
      try { this.remoteAudio.srcObject = null; } catch { /* ignore */ }
      // Keep element mounted to preserve autoplay-unlock state for next call.
    }
    if (this.remoteStream) {
      this.remoteStream.getTracks().forEach((t) => { try { t.stop(); } catch { /* ignore */ } });
      this.remoteStream = null;
    }
    if (this.localStream) {
      this.localStream.getTracks().forEach((t) => { try { t.stop(); } catch { /* ignore */ } });
      this.localStream = null;
    }
    const pc = this.session?.connection;
    if (pc) {
      try { pc.getReceivers().forEach((r) => r.track && r.track.stop()); } catch { /* ignore */ }
      try { pc.close(); } catch { /* ignore */ }
    }
  }

  setMuted(m: boolean) {
    if (!this.session || !this.active) return;
    if (m) this.session.mute({ audio: true });
    else this.session.unmute({ audio: true });
    this.active = { ...this.active, muted: m };
    this.emitCall();
  }

  async setHold(h: boolean) {
    if (!this.session || !this.active) return;
    if (h) this.session.hold();
    else this.session.unhold();
    this.active = { ...this.active, onHold: h };
    this.emitCall();
  }

  sendDtmf(tone: string) {
    if (!this.session) return;
    try {
      this.session.sendDTMF(tone);
    } catch (err) {
      this.debug("warn", "DTMF failed", err);
    }
  }

  async blindTransfer(target: string) {
    if (!this.session) return;
    const tenantTarget = withTenantExtension(target);
    const fullTarget = tenantTarget.includes("@")
      ? tenantTarget
      : `sip:${tenantTarget.replace(/[^\w+#*\-.]/g, "")}@${this.uaHost()}`;
    this.debug("info", `[REFER] target=${fullTarget}`, { rawTarget: target, tenantTarget, fullTarget });
    console.info("[SIP][REFER] target", { rawTarget: target, tenantTarget, fullTarget });
    try {
      this.session.refer(fullTarget);
    } catch (err) {
      this.debug("error", "Transfer failed", err);
      throw new Error("Transfer failed");
    }
  }

  getRemoteStream() {
    return this.remoteStream;
  }
  getLocalStream() {
    return this.localStream;
  }
  getPeerConnection() {
    return this.session?.connection ?? null;
  }
}

let singleton: SipAdapter | null = null;
export function getSipAdapter(): SipAdapter {
  if (!singleton) singleton = new SipService();
  return singleton;
}
export function setSipAdapter(adapter: SipAdapter) {
  singleton = adapter;
}
