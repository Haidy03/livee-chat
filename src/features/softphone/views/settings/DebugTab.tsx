/** Live SIP debug log — useful for diagnosing registration/call issues. */

import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Copy } from "lucide-react";
import { useSipState } from "../../sip/useSipState";
import { cn } from "@/lib/utils";

interface TargetEntry {
  at: number;
  kind: "INVITE" | "REFER";
  rawTarget: string;
  fullTarget: string;
  displayName?: string;
}

interface TimelineEvent {
  at: number;
  label: string;
  kind: "dial" | "ice" | "wire" | "info";
  detail?: string;
}

interface DialTimeline {
  startedAt: number;
  events: TimelineEvent[];
  inviteSentAt?: number;
}

export function DebugTab() {
  const { t } = useTranslation();
  const { debugLog } = useSipState();

  const targets = useMemo<TargetEntry[]>(() => {
    const out: TargetEntry[] = [];
    for (const e of debugLog) {
      const m = /^\[(INVITE|REFER)\] target=/.exec(e.message);
      if (!m) continue;
      const d = (e.detail ?? {}) as {
        rawTarget?: string;
        fullTarget?: string;
        displayName?: string;
      };
      out.push({
        at: e.at,
        kind: m[1] as "INVITE" | "REFER",
        rawTarget: d.rawTarget ?? "",
        fullTarget: d.fullTarget ?? "",
        displayName: d.displayName,
      });
    }
    return out.slice(-20).reverse();
  }, [debugLog]);

  // Build dial timelines: each [DIAL] click starts a new group; subsequent
  // [DIAL]/[ICE] events and the next "SIP → INVITE" wire send attach to it.
  const timelines = useMemo<DialTimeline[]>(() => {
    const groups: DialTimeline[] = [];
    let current: DialTimeline | null = null;
    for (const e of debugLog) {
      const msg = e.message;
      const dtMs = (e.detail as { dtMs?: number } | undefined)?.dtMs;
      const dtStr = typeof dtMs === "number" ? `+${dtMs}ms` : undefined;
      if (msg === "[DIAL] click") {
        current = { startedAt: e.at, events: [{ at: e.at, label: "dial click", kind: "dial" }] };
        groups.push(current);
        continue;
      }
      if (!current) continue;
      if (msg.startsWith("[DIAL] ")) {
        current.events.push({ at: e.at, label: msg.slice(7), kind: "dial", detail: dtStr });
      } else if (msg.startsWith("[ICE] ")) {
        current.events.push({ at: e.at, label: msg.slice(6), kind: "ice", detail: dtStr });
      } else if (msg.startsWith("[INVITE] target=")) {
        current.events.push({ at: e.at, label: "INVITE prepared", kind: "info" });
      } else if (msg.startsWith("SIP → INVITE") && current.inviteSentAt == null) {
        current.inviteSentAt = e.at;
        current.events.push({
          at: e.at,
          label: "INVITE on the wire",
          kind: "wire",
          detail: `+${e.at - current.startedAt}ms`,
        });
      }
    }
    return groups.slice(-5).reverse();
  }, [debugLog]);

  const copy = async (s: string) => {
    try {
      await navigator.clipboard.writeText(s);
      toast.success("Copied SIP target");
    } catch {
      toast.error("Copy failed");
    }
  };

  return (
    <div className="space-y-4">
      {/* Dial → INVITE timeline */}
      <div className="rounded-2xl bg-card border border-border/60 p-3">
        <div className="text-sm font-semibold p-2">
          {t("softphone.settings.dialTimeline", "Dial → INVITE timeline")}
        </div>
        <div className="text-xs text-muted-foreground px-2 pb-2">
          Time from clicking dial to the SIP INVITE actually being transmitted, plus ICE gathering steps.
        </div>
        <div className="max-h-[320px] overflow-auto rounded-lg bg-muted/40 p-2 space-y-2">
          {timelines.length === 0 ? (
            <div className="text-xs text-muted-foreground p-2">
              No dial recorded yet. Place a call to see the timeline.
            </div>
          ) : (
            timelines.map((tl, i) => {
              const total = tl.inviteSentAt ? tl.inviteSentAt - tl.startedAt : null;
              return (
                <div key={i} className="rounded-md border border-border/60 bg-background p-2 text-xs">
                  <div className="flex items-center gap-2 mb-2">
                    <span className="text-muted-foreground tabular">
                      {new Date(tl.startedAt).toLocaleTimeString()}
                    </span>
                    <span
                      className={cn(
                        "ms-auto px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tabular",
                        total == null
                          ? "bg-muted text-muted-foreground"
                          : total < 1000
                            ? "bg-emerald-500/15 text-emerald-600"
                            : total < 5000
                              ? "bg-amber-500/15 text-amber-600"
                              : "bg-destructive/15 text-destructive",
                      )}
                    >
                      {total == null ? "pending" : `dial → wire: ${total}ms`}
                    </span>
                  </div>
                  <ol className="space-y-0.5 font-mono">
                    {tl.events.map((ev, j) => (
                      <li key={j} className="flex gap-2">
                        <span
                          className={cn(
                            "uppercase text-[10px] font-semibold w-12 shrink-0",
                            ev.kind === "dial"
                              ? "text-primary"
                              : ev.kind === "ice"
                                ? "text-blue-600"
                                : ev.kind === "wire"
                                  ? "text-emerald-600"
                                  : "text-muted-foreground",
                          )}
                        >
                          {ev.kind}
                        </span>
                        <span className="flex-1 break-all">{ev.label}</span>
                        {ev.detail && (
                          <span className="text-muted-foreground tabular shrink-0">{ev.detail}</span>
                        )}
                      </li>
                    ))}
                  </ol>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* SIP target inspector */}
      <div className="rounded-2xl bg-card border border-border/60 p-3">
        <div className="text-sm font-semibold p-2">

          {t("softphone.settings.sipTargets", "SIP targets (INVITE / REFER)")}
        </div>
        <div className="text-xs text-muted-foreground px-2 pb-2">
          The exact target URI generated by the app right before sending each request.
        </div>
        <div className="max-h-[260px] overflow-auto rounded-lg bg-muted/40 p-2 space-y-2">
          {targets.length === 0 ? (
            <div className="text-xs text-muted-foreground p-2">
              No outbound INVITE/REFER yet. Place a call to see the generated target here.
            </div>
          ) : (
            targets.map((tg, i) => (
              <div
                key={i}
                className="rounded-md border border-border/60 bg-background p-2 text-xs"
              >
                <div className="flex items-center gap-2 mb-1">
                  <span
                    className={cn(
                      "px-1.5 py-0.5 rounded text-[10px] font-bold uppercase",
                      tg.kind === "INVITE"
                        ? "bg-emerald-500/15 text-emerald-600"
                        : "bg-blue-500/15 text-blue-600",
                    )}
                  >
                    {tg.kind}
                  </span>
                  <span className="text-muted-foreground tabular">
                    {new Date(tg.at).toLocaleTimeString()}
                  </span>
                  {tg.displayName && (
                    <span className="text-muted-foreground truncate">
                      “{tg.displayName}”
                    </span>
                  )}
                  <button
                    onClick={() => copy(tg.fullTarget)}
                    className="ms-auto inline-flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                    title="Copy target"
                  >
                    <Copy className="h-3 w-3" />
                    Copy
                  </button>
                </div>
                <div className="font-mono break-all" dir="ltr">
                  <span className="text-muted-foreground">target: </span>
                  <span className="font-semibold">{tg.fullTarget}</span>
                </div>
                {tg.rawTarget && tg.rawTarget !== tg.fullTarget && (
                  <div className="font-mono break-all text-muted-foreground mt-0.5" dir="ltr">
                    raw: {tg.rawTarget}
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>

      {/* Full debug log */}
      <div className="rounded-2xl bg-card border border-border/60 p-3">
        <div className="text-sm font-semibold p-2">
          {t("softphone.settings.debug", "Debug log")}
        </div>
        <div className="font-mono text-xs max-h-[420px] overflow-auto bg-muted/40 rounded-lg p-3">
          {debugLog.length === 0 ? (
            <div className="text-muted-foreground">
              {t("softphone.settings.debugEmpty", "No events yet.")}
            </div>
          ) : (
            debugLog.map((e, i) => (
              <div key={i} className="flex gap-3">
                <span className="text-muted-foreground tabular shrink-0">
                  {new Date(e.at).toLocaleTimeString()}
                </span>
                <span
                  className={cn(
                    "uppercase text-[10px] font-semibold w-10 shrink-0",
                    e.level === "error"
                      ? "text-destructive"
                      : e.level === "warn"
                        ? "text-amber-500"
                        : "text-muted-foreground",
                  )}
                >
                  {e.level}
                </span>
                <span className="break-all">{e.message}</span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
