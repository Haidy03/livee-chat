import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";
import { Wifi, WifiOff, Loader2, AlertCircle } from "lucide-react";
import { useRealtimeStore } from "../stores";

export function ConnectionStatusPill({ className }: { className?: string }) {
  const state = useRealtimeStore((s) => s.connectionState);
  const [pulse, setPulse] = useState(false);
  useEffect(() => {
    setPulse(true);
    const t = setTimeout(() => setPulse(false), 800);
    return () => clearTimeout(t);
  }, [state]);
  const map: Record<typeof state, { icon: any; label: string; cls: string }> = {
    connected: { icon: Wifi, label: "Connected", cls: "text-emerald-600 bg-emerald-50 dark:bg-emerald-950/40" },
    connecting: { icon: Loader2, label: "Connecting…", cls: "text-amber-600 bg-amber-50 dark:bg-amber-950/40 animate-pulse" },
    reconnecting: { icon: Loader2, label: "Reconnecting…", cls: "text-amber-600 bg-amber-50 dark:bg-amber-950/40 animate-pulse" },
    disconnected: { icon: WifiOff, label: "Disconnected", cls: "text-muted-foreground bg-muted" },
    failed: { icon: AlertCircle, label: "Connection failed", cls: "text-destructive bg-destructive/10" },
  };
  const { icon: Icon, label, cls } = map[state];
  return (
    <span
      className={cn("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium", cls, pulse && "ring-2 ring-current/30", className)}
      role="status"
      aria-live="polite"
    >
      <Icon className={cn("h-3 w-3", state === "connecting" || state === "reconnecting" ? "animate-spin" : "")} aria-hidden />
      {label}
    </span>
  );
}
