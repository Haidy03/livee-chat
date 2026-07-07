import { Clock } from "lucide-react";
import { useAgentConfigStore, useLiveChatTimerStore } from "../stores";
import { formatDuration } from "@/lib/duration";
import { cn } from "@/lib/utils";

interface Props {
  roomId: string;
  className?: string;
}

function timerTone(current: number, max: number, forceRed = false): string {
  if (forceRed) return "text-destructive font-semibold";
  if (!max || max <= 0) return "text-emerald-600 dark:text-emerald-400 font-medium";
  return current >= max * 0.8
    ? "text-destructive font-semibold"
    : "text-emerald-600 dark:text-emerald-400 font-medium";
}

/**
 * Renders per-conversation inactivity timers (client silent / agent silent /
 * offline). Always renders at least an idle 0:00 chip when config is loaded,
 * so every conversation row shows a timer.
 */
export function ConversationTimers({ roomId, className }: Props) {
  const span = useLiveChatTimerStore((s) => s.byRoom[roomId]);
  const maxInActiveClient = useAgentConfigStore((s) => s.maxInActiveClient);
  const maxAgentInActiveWithClient = useAgentConfigStore((s) => s.maxAgentInActiveWithClient);
  const clientDisconnectedTimeout = useAgentConfigStore((s) => s.clientDisconnectedTimeout);
  const loaded = useAgentConfigStore((s) => s.loaded);

  if (!loaded) return null;

  const agentMessageSpan = span?.agentMessageSpan ?? 0;
  const utteranceSpan = span?.utteranceSpan ?? 0;
  const offlineSpan = span?.offlineSpan ?? 0;
  const notifyPending = span?.notifyPending ?? false;
  const deleted = span?.deleted ?? false;

  const hasAny = agentMessageSpan > 0 || utteranceSpan > 0 || offlineSpan > 0 || deleted;

  return (
    <div
      className={cn(
        "mt-0.5 flex flex-wrap items-center gap-1.5 text-[10px] tabular-nums",
        className,
      )}
    >
      {!hasAny && (
        <span className="inline-flex items-center gap-1 text-muted-foreground">
          <Clock className="h-3 w-3" aria-hidden />
          {formatDuration(0)}
        </span>
      )}
      {agentMessageSpan > 0 && (
        <span
          className={cn(
            "inline-flex items-center gap-1",
            timerTone(agentMessageSpan, maxInActiveClient, notifyPending),
          )}
        >
          <Clock className="h-3 w-3" aria-hidden />
          Client inactive: {formatDuration(agentMessageSpan)}
        </span>
      )}
      {utteranceSpan > 0 && (
        <span className={timerTone(utteranceSpan, maxAgentInActiveWithClient)}>
          Agent inactive: {formatDuration(utteranceSpan)}
        </span>
      )}
      {offlineSpan > 0 && (
        <span className={timerTone(offlineSpan, clientDisconnectedTimeout, true)}>
          Client offline: {formatDuration(offlineSpan)}
        </span>
      )}
      {deleted && <span className="text-destructive font-semibold">Closing…</span>}
    </div>
  );
}
