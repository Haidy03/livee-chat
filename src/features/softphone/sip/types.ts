/**
 * SIP / softphone types.
 * Kept framework-agnostic so SipService and demoAdapter can both implement them.
 */

export interface SipConfig {
  displayName: string;
  sipUri: string;        // e.g. sip:alice@example.com
  authId: string;        // SIP auth username
  password: string;      // never logged, never persisted server-side
  wsUrl: string;         // wss://sip.example.com:7443
  stunUrls: string[];
  turnUrl?: string;
  turnUsername?: string;
  turnPassword?: string;
}

export type RegistrationStatus =
  | "unregistered"
  | "connecting"
  | "registered"
  | "failed";

export type CallPhase =
  | "idle"
  | "outbound_dialing"
  | "outbound_ringing"
  | "inbound_ringing"
  | "active"
  | "ended"
  | "failed";

export interface ActiveSipCall {
  id: string;
  /** SIP Call-ID — same value used by Asterisk as the recording filename. */
  callId: string;
  remoteNumber: string;
  remoteDisplayName: string;
  /** Full From-header URI of the remote party (inbound) or local (outbound). */
  fromUri?: string;
  direction: "in" | "out";
  startedAt: number;       // when the session started (ms)
  connectedAt: number | null; // when media was actually connected
  phase: CallPhase;
  muted: boolean;
  onHold: boolean;
  failureReason?: string;
}

export interface QualitySnapshot {
  bars: 0 | 1 | 2 | 3 | 4;       // 0 = bad, 4 = excellent
  rttMs: number | null;
  jitterMs: number | null;
  packetLossPct: number | null;
}

export interface SipDebugEvent {
  at: number;
  level: "info" | "warn" | "error";
  message: string;
  detail?: unknown;
}

/** Public surface for both the real and the demo SIP adapter. */
export interface SipAdapter {
  register(cfg: SipConfig): Promise<void>;
  unregister(): Promise<void>;
  call(target: string, displayName?: string): Promise<void>;
  answer(): Promise<void>;
  reject(): Promise<void>;
  hangup(): Promise<void>;
  setMuted(m: boolean): void;
  setHold(h: boolean): Promise<void>;
  sendDtmf(tone: string): void;
  /** Blind transfer — the call is forwarded; we drop. */
  blindTransfer(target: string): Promise<void>;
  setSinkId?(sinkId: string): Promise<void>;
  /** Pre-create + unlock the remote <audio> element inside a user gesture. */
  prewarmAudio?(): void;
  /** Returns the remote audio MediaStream for visualizers. */
  getRemoteStream(): MediaStream | null;
  /** Returns the local mic MediaStream. */
  getLocalStream(): MediaStream | null;
  /** RTCPeerConnection used by the active session, for getStats(). */
  getPeerConnection(): RTCPeerConnection | null;
  on(event: SipEventName, handler: SipEventHandler): () => void;
}

export type SipEventName =
  | "registration"
  | "call"
  | "debug"
  | "callRecord";

export type CallRecordStatus =
  | "ringing"
  | "in-progress"
  | "completed"
  | "missed"
  | "failed"
  | "rejected"
  | "busy";

export interface CallRecordEvent {
  callId: string;
  patch: {
    status?: CallRecordStatus;
    direction?: "in" | "out";
    from_uri?: string;
    from_display?: string | null;
    to_uri?: string;
    to_display?: string | null;
    started_at?: string;
    answered_at?: string;
    ended_at?: string;
    hangup_cause?: string;
    recording_url?: string | null;
    has_recording?: boolean;
    caller?: string;
    called?: string;
    ring_seconds?: number;
    hold_seconds?: number;
    active_seconds?: number;
    total_hold_seconds?: number;
    total_seconds?: number;
  };
}

export type SipEventHandler = (payload: unknown) => void;

export interface RegistrationEvent {
  status: RegistrationStatus;
  reason?: string;
}

export interface CallEvent {
  call: ActiveSipCall | null;
}
