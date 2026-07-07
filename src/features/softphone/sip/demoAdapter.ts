/**
 * Demo SIP adapter — same surface as SipService but no network.
 * Lets reviewers exercise the full UI (register, outbound, inbound, mute, hold, DTMF, hangup)
 * without provisioning a SIP server.
 */

import { SipEventBus } from "./eventBus";
import type {
  ActiveSipCall,
  CallEvent,
  RegistrationEvent,
  SipAdapter,
  SipConfig,
  SipDebugEvent,
  SipEventHandler,
  SipEventName,
} from "./types";

export class DemoSipAdapter implements SipAdapter {
  private bus = new SipEventBus();
  private active: ActiveSipCall | null = null;
  private timers = new Set<number>();
  private localStream: MediaStream | null = null;

  on(event: SipEventName, handler: SipEventHandler): () => void {
    return this.bus.on(event, handler);
  }

  private debug(message: string, level: SipDebugEvent["level"] = "info") {
    this.bus.emit("debug", { at: Date.now(), level, message } satisfies SipDebugEvent);
  }
  private emitReg(status: RegistrationEvent["status"], reason?: string) {
    this.bus.emit("registration", { status, reason } satisfies RegistrationEvent);
  }
  private emitCall() {
    this.bus.emit("call", { call: this.active } satisfies CallEvent);
  }
  private wait(ms: number, fn: () => void) {
    const id = window.setTimeout(() => {
      this.timers.delete(id);
      fn();
    }, ms);
    this.timers.add(id);
  }

  async register(_cfg: SipConfig): Promise<void> {
    this.emitReg("connecting");
    this.debug("[demo] connecting…");
    this.wait(700, () => {
      this.emitReg("registered");
      this.debug("[demo] registered");
    });
  }

  async unregister(): Promise<void> {
    this.timers.forEach((id) => clearTimeout(id));
    this.timers.clear();
    this.active = null;
    this.emitCall();
    this.emitReg("unregistered");
  }

  async call(target: string): Promise<void> {
    if (this.active) throw new Error("Another call is already in progress.");
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      // Demo mode: continue even without mic
      this.localStream = null;
    }
    const id = "demo-" + Date.now();
    this.active = {
      id,
      callId: id,
      remoteNumber: target,
      remoteDisplayName: "",
      direction: "out",
      startedAt: Date.now(),
      connectedAt: null,
      phase: "outbound_dialing",
      muted: false,
      onHold: false,
    };
    this.emitCall();
    this.wait(600, () => {
      if (!this.active) return;
      this.active = { ...this.active, phase: "outbound_ringing" };
      this.emitCall();
    });
    this.wait(1800, () => {
      if (!this.active) return;
      this.active = { ...this.active, phase: "active", connectedAt: Date.now() };
      this.emitCall();
    });
  }

  /** Trigger a fake inbound call. */
  simulateInbound(number: string, displayName: string) {
    if (this.active) return;
    const id = "demo-in-" + Date.now();
    this.active = {
      id,
      callId: id,
      remoteNumber: number,
      remoteDisplayName: displayName,
      direction: "in",
      startedAt: Date.now(),
      connectedAt: null,
      phase: "inbound_ringing",
      muted: false,
      onHold: false,
    };
    this.emitCall();
  }

  async answer(): Promise<void> {
    if (!this.active || this.active.phase !== "inbound_ringing") return;
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      this.localStream = null;
    }
    this.active = { ...this.active, phase: "active", connectedAt: Date.now() };
    this.emitCall();
  }
  async reject(): Promise<void> {
    this.endNow("ended", "Rejected");
  }
  async hangup(): Promise<void> {
    this.endNow("ended");
  }
  private endNow(phase: "ended" | "failed", reason?: string) {
    if (!this.active) return;
    this.active = { ...this.active, phase, failureReason: reason };
    this.emitCall();
    if (this.localStream) {
      this.localStream.getTracks().forEach((t) => t.stop());
      this.localStream = null;
    }
    this.wait(1000, () => {
      this.active = null;
      this.emitCall();
    });
  }

  setMuted(m: boolean) {
    if (!this.active) return;
    this.active = { ...this.active, muted: m };
    this.emitCall();
  }
  async setHold(h: boolean) {
    if (!this.active) return;
    this.active = { ...this.active, onHold: h };
    this.emitCall();
  }
  sendDtmf(tone: string) {
    this.debug(`[demo] DTMF ${tone}`);
  }
  async blindTransfer(target: string) {
    this.debug(`[demo] transfer to ${target}`);
    this.endNow("ended", "Transferred");
  }
  getRemoteStream() {
    return null;
  }
  getLocalStream() {
    return this.localStream;
  }
  getPeerConnection() {
    return null;
  }
}
