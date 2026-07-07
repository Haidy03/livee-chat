import { useEffect } from "react";
import { useSoftphone } from "../store";
import { playDtmfTone } from "../sip/dtmfTone";

export function useDialpadKeyboard(onCall: () => void) {
  const { appendDigit, backspace, dialed, activeCall } = useSoftphone();
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (activeCall) return;
      const target = e.target as HTMLElement | null;
      if (target && (target.tagName === "INPUT" || target.tagName === "TEXTAREA" || target.isContentEditable)) return;
      if (/^[0-9*#]$/.test(e.key)) {
        e.preventDefault();
        appendDigit(e.key);
        playDtmfTone(e.key);
      } else if (e.key === "Backspace") {
        e.preventDefault();
        backspace();
      } else if (e.key === "Enter" && dialed) {
        e.preventDefault();
        onCall();
      } else if (e.key === "+" && dialed.length === 0) {
        e.preventDefault();
        appendDigit("+");
        playDtmfTone("+");
      }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [appendDigit, backspace, dialed, onCall, activeCall]);
}

