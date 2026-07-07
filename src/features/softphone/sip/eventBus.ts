import type { SipEventName, SipEventHandler } from "./types";

/** Tiny pub-sub used by both real and demo adapters. */
export class SipEventBus {
  private listeners = new Map<SipEventName, Set<SipEventHandler>>();

  on(event: SipEventName, handler: SipEventHandler): () => void {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set());
    this.listeners.get(event)!.add(handler);
    return () => this.listeners.get(event)?.delete(handler);
  }

  emit(event: SipEventName, payload: unknown) {
    this.listeners.get(event)?.forEach((h) => {
      try {
        h(payload);
      } catch (err) {
        // never let a handler crash the bus
        console.error("[sip] event handler error", err);
      }
    });
  }
}
