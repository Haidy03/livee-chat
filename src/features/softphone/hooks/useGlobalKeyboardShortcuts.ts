import { useEffect } from "react";
import { sipActions } from "../sip/useSipController";
import { useSipState } from "../sip/useSipState";

/**
 * Global keyboard shortcuts:
 *  - Space  → answer (only when ringing)
 *  - Esc    → hangup / reject
 *  - M      → toggle mute
 * Ignores when the focus is in an input/textarea/contenteditable field.
 */
export function useGlobalKeyboardShortcuts() {
  const { call } = useSipState();

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      if (target) {
        const tag = target.tagName;
        if (tag === "INPUT" || tag === "TEXTAREA" || target.isContentEditable) return;
      }
      if (!call) return;

      if (e.key === " " || e.code === "Space") {
        if (call.phase === "inbound_ringing") {
          e.preventDefault();
          sipActions.answer().catch(() => {});
        }
      } else if (e.key === "Escape") {
        if (call.phase === "active" || call.phase === "outbound_dialing" || call.phase === "outbound_ringing") {
          e.preventDefault();
          sipActions.hangup();
        } else if (call.phase === "inbound_ringing") {
          e.preventDefault();
          sipActions.reject();
        }
      } else if (e.key === "m" || e.key === "M") {
        if (call.phase === "active") {
          e.preventDefault();
          sipActions.toggleMute(!call.muted);
        }
      }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [call]);
}
