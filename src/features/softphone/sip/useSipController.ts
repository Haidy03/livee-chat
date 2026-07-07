/**
 * Bridges the real SIP adapter with the existing Zustand `useSoftphone` store
 * and the wider app (toasts, browser notifications, ringtone, call logs).
 *
 * Mounted once at the top of the Softphone page.
 */

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useSoftphone } from "../store";
import { getSipAdapter, useSipState } from "../sip/useSipState";
import { startRingtone, stopRingtone, startRingback, stopRingback } from "../sip/ringtone";
import { findContactByNumberFromCache } from "../hooks/useSoftphoneContacts";
import { persistCallEvent } from "./callsRepo";
import type { CallRecordEvent } from "./types";

export function useSipController() {
  const sip = useSipState();
  const qc = useQueryClient();
  const setActiveCall = useSoftphone((s) => s.setActiveCall);
  const pushHistoryRecord = useSoftphone((s) => s.pushHistoryRecord);

  // Persist every SIP call lifecycle event into the shared `calls` table,
  // keyed by SIP Call-ID. The DB row is created on "ringing" so calls
  // appear in history even if the browser crashes mid-call.
  useEffect(() => {
    const adapter = getSipAdapter();
    const off = adapter.on("callRecord", (p) => {
      void persistCallEvent(p as CallRecordEvent);
    });
    return () => off();
  }, []);

  // Warn before unload while a call is active, and persist a recovery
  // breadcrumb so the next page load can show "your call was interrupted".
  useEffect(() => {
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      const call = sip.call;
      if (!call) return;
      try {
        localStorage.setItem(
          "softphone:interrupted-call",
          JSON.stringify({
            number: call.remoteNumber,
            direction: call.direction,
            startedAt: call.startedAt,
            connectedAt: call.connectedAt,
          }),
        );
      } catch { /* ignore */ }
      // Best-effort: tell the SIP server we're gone so no ghost call lingers.
      try { void getSipAdapter().hangup(); } catch { /* ignore */ }
      e.preventDefault();
      e.returnValue = "";
      return "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [sip.call]);

  const lastPhaseRef = useRef<string>("idle");
  const startedAtRef = useRef<number>(0);
  const connectedAtRef = useRef<number | null>(null);

  // Mirror SIP call state into the legacy useSoftphone.activeCall shape so existing views work.
  useEffect(() => {
    const c = sip.call;
    if (!c) {
      setActiveCall(null);
      lastPhaseRef.current = "idle";
      stopRingtone();
      stopRingback();
      return;
    }

    // Keep references for log on end
    if (c.phase === "outbound_dialing" || c.phase === "inbound_ringing") {
      startedAtRef.current = c.startedAt;
    }
    if (c.phase === "active" && !connectedAtRef.current) {
      connectedAtRef.current = c.connectedAt ?? Date.now();
    }

    // Ringtone for inbound, ringback for outbound dialing
    if (c.phase === "inbound_ringing") {
      stopRingback();
      startRingtone("classic");
    } else if (c.phase === "outbound_dialing") {
      stopRingtone();
      startRingback();
    } else {
      stopRingtone();
      stopRingback();
    }

    const contact = findContactByNumberFromCache(qc, c.remoteNumber);
    setActiveCall({
      contactId: contact?.id,
      number: c.remoteNumber,
      direction: c.direction,
      startedAt: c.connectedAt ?? c.startedAt,
      muted: c.muted,
      onHold: c.onHold,
      recording: false,
      speaker: false,
      showKeypad: false,
    });

    // Browser notification on inbound when tab not focused
    if (c.phase === "inbound_ringing" && lastPhaseRef.current !== "inbound_ringing") {
      try {
        if ("Notification" in window && Notification.permission === "granted" && document.visibilityState !== "visible") {
          new Notification("Incoming call", { body: contact?.name || c.remoteNumber, tag: "softphone-incoming" });
        }
      } catch {
        /* ignore */
      }
    }

    // On terminal phases, log the call
    if ((c.phase === "ended" || c.phase === "failed") && lastPhaseRef.current !== c.phase) {
      const wasAnswered = !!connectedAtRef.current;
      const status =
        c.phase === "failed"
          ? "failed"
          : !wasAnswered && c.direction === "in"
            ? "missed"
            : !wasAnswered && c.failureReason === "Rejected"
              ? "rejected"
              : "answered";
      const durationSec = wasAnswered && connectedAtRef.current ? Math.floor((Date.now() - connectedAtRef.current) / 1000) : 0;
      const histType = c.direction === "in" ? (status === "missed" ? "missed" : "in") : "out";

      pushHistoryRecord({
        id: "r" + Date.now(),
        contactId: contact?.id,
        number: c.remoteNumber,
        type: histType,
        at: startedAtRef.current || Date.now(),
        durationSec,
      });

      // Note: durable persistence happens via the "callRecord" SIP event →
      // persistCallEvent() upserting into the shared `calls` table by Call-ID.

      if (status === "failed") {
        toast.error(c.failureReason || "Call failed");
      }
      connectedAtRef.current = null;
    }

    lastPhaseRef.current = c.phase;
  }, [sip.call, setActiveCall, pushHistoryRecord, qc]);

  // Surface registration failures via toast
  const lastRegRef = useRef(sip.registration);
  useEffect(() => {
    if (sip.registration === "failed" && lastRegRef.current !== "failed") {
      toast.error(sip.registrationReason || "SIP registration failed");
    }
    if (sip.registration === "registered" && lastRegRef.current !== "registered") {
      toast.success("SIP account registered");
    }
    lastRegRef.current = sip.registration;
  }, [sip.registration, sip.registrationReason]);

  return sip;
}

/** Convenience helpers used by UI buttons. */
export const sipActions = {
  call: (target: string, displayName?: string) => getSipAdapter().call(target, displayName),
  answer: () => getSipAdapter().answer(),
  reject: () => getSipAdapter().reject(),
  hangup: () => getSipAdapter().hangup(),
  toggleMute: (m: boolean) => getSipAdapter().setMuted(m),
  toggleHold: (h: boolean) => getSipAdapter().setHold(h),
  dtmf: (tone: string) => getSipAdapter().sendDtmf(tone),
  transfer: (target: string) => getSipAdapter().blindTransfer(target),
};
