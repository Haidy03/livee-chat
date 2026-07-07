import { AlertTriangle, Clock, CheckCircle2 } from "lucide-react";
import type { RoomSLA } from "../models";
import { cn } from "@/lib/utils";
import { useEffect, useState } from "react";

interface Props {
  sla: RoomSLA;
  className?: string;
}

function formatRemaining(targetIso?: string) {
  if (!targetIso) return "";
  const diff = +new Date(targetIso) - Date.now();
  const abs = Math.abs(diff);
  const m = Math.floor(abs / 60000);
  const s = Math.floor((abs % 60000) / 1000);
  const sign = diff < 0 ? "-" : "";
  return `${sign}${m}m ${s.toString().padStart(2, "0")}s`;
}

export function SLAIndicator({ sla, className }: Props) {
  const [, setTick] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setTick((t) => t + 1), 1000);
    return () => clearInterval(id);
  }, []);

  const Icon = sla.state === "ok" ? CheckCircle2 : sla.state === "warning" ? Clock : AlertTriangle;
  const cls =
    sla.state === "ok"
      ? "text-emerald-600 dark:text-emerald-400"
      : sla.state === "warning"
        ? "text-amber-600 dark:text-amber-400"
        : "text-destructive";
  const remaining = formatRemaining(sla.firstResponseDeadline ?? sla.resolutionDeadline);
  return (
    <span
      className={cn("inline-flex items-center gap-1 text-xs font-medium tabular-nums", cls, className)}
      aria-label={`SLA ${sla.state} ${remaining}`}
    >
      <Icon className="h-3.5 w-3.5" aria-hidden />
      {remaining}
    </span>
  );
}
