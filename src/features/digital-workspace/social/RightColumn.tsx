import {
  Bell, Send, X as XIcon, MoreHorizontal, BadgeCheck, Clock,
  UserRound, BarChart3, History, Package, BookOpen, Sparkles, Check,
  Heart, Eye, Repeat as RepeatIcon, TrendingUp,
} from "lucide-react";
import { Avatar, Button, IconButton, PlatformBadge, Pill, SectionLabel, SentimentDot } from "./ui/primitives";
import { STATS, AUTHOR_ACTIVITY } from "./data";
import { SiX } from "react-icons/si";

const STAT_ICON = { eye: Eye, heart: Heart, repeat: RepeatIcon, trending: TrendingUp } as const;
const ACT_ICON = { x: SiX, send: Send, heart: Heart } as const;

export function RightColumn() {
  return (
    <aside
      className="flex flex-col min-h-0"
      style={{ width: 348, background: "var(--si-panel)", borderLeft: "1px solid var(--si-border)" }}
    >
      {/* Actions row */}
      <div
        className="flex items-center gap-2"
        style={{ padding: "14px 16px 12px", borderBottom: "1px solid var(--si-border)" }}
      >
        <Avatar initials="JD" color="rose" size={32} />
        <div className="flex-1"><Button variant="primary" icon={Check} className="w-full">Resolve</Button></div>
        <Button size="sm" variant="ghost" icon={RepeatIcon}>Assign</Button>
        <IconButton aria-label="More"><MoreHorizontal size={16} /></IconButton>
      </div>

      {/* Quick actions */}
      <div className="flex gap-2" style={{ padding: "12px 16px 6px" }}>
        {[
          { Icon: Bell, label: "Follow" },
          { Icon: Send, label: "DM" },
          { Icon: XIcon, label: "Hide" },
        ].map(({ Icon, label }) => (
          <button
            key={label}
            type="button"
            className="flex-1 inline-flex items-center justify-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)]"
            style={{
              height: 38, border: "1px solid var(--si-border-2)", borderRadius: 10,
              fontSize: 12.5, fontWeight: 600, color: "var(--si-text)", background: "var(--si-panel)",
            }}
          >
            <Icon size={14} style={{ color: "var(--si-text-2)" }} /> {label}
          </button>
        ))}
      </div>

      {/* Identity */}
      <div style={{ padding: "12px 16px 6px" }}>
        <div className="flex items-center gap-1.5">
          <span style={{ fontSize: 16, fontWeight: 700 }}>Jenna Doyle</span>
          <BadgeCheck size={14} style={{ color: "#1d9bf0", fill: "#1d9bf0" }} className="text-white" />
          <div className="ms-auto"><PlatformBadge platform="x" size={24} /></div>
        </div>
        <div className="flex flex-wrap" style={{ gap: "4px 14px", marginTop: 6, fontSize: 12, color: "var(--si-text-2)" }}>
          <span>Handle: <strong style={{ color: "var(--si-text)" }}>@jenna_d</strong></span>
          <span>Followers: <strong style={{ color: "var(--si-text)" }}>12.4K</strong></span>
          <span>Customer: <strong style={{ color: "var(--si-text)" }}>Yes · #80421</strong></span>
          <span>Sentiment: <strong style={{ color: "#b91c1c" }}>Negative</strong></span>
        </div>
      </div>

      {/* Status row */}
      <div
        className="flex flex-wrap items-center gap-2"
        style={{ padding: "12px 16px", borderBottom: "1px solid var(--si-border)" }}
      >
        <Pill variant="red">Needs reply</Pill>
        <Pill variant="purple">High reach</Pill>
        <Pill variant="default" icon={Clock}>-2m 14s</Pill>
      </div>

      {/* Tab strip */}
      <div
        className="flex items-center"
        style={{ padding: "8px 12px", borderBottom: "1px solid var(--si-border)", gap: 2 }}
      >
        {[
          { Icon: UserRound, label: "Author" },
          { Icon: BarChart3, label: "Performance", active: true },
          { Icon: History, label: "History" },
          { Icon: Package, label: "Orders" },
          { Icon: BookOpen, label: "Knowledge" },
          { Icon: Sparkles, label: "AI" },
        ].map(({ Icon, label, active }) => (
          <button
            key={label}
            type="button"
            aria-label={label}
            aria-pressed={!!active}
            className="inline-flex items-center justify-center transition-colors"
            style={{
              width: 36, height: 32, borderRadius: 8,
              background: active ? "#eef7f0" : "transparent",
              color: active ? "var(--si-brand-soft-tx)" : "var(--si-text-3)",
            }}
          >
            <Icon size={15} />
          </button>
        ))}
      </div>

      {/* Scroll: stats + activity */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: "14px 16px" }}>
        <SectionLabel>This mention's performance</SectionLabel>
        <div className="grid grid-cols-2" style={{ gap: 10 }}>
          {STATS.map((s) => {
            const Icon = STAT_ICON[s.icon as keyof typeof STAT_ICON];
            return (
              <div
                key={s.label}
                style={{
                  border: "1px solid var(--si-border)", borderRadius: 12, padding: 12,
                  background: "var(--si-panel-2)",
                }}
              >
                <div className="tnum" style={{ fontSize: 20, fontWeight: 700, color: "var(--si-text)" }}>{s.value}</div>
                <div className="inline-flex items-center gap-1" style={{ fontSize: 11.5, color: "var(--si-text-2)", marginTop: 2 }}>
                  <Icon size={13} /> {s.label}
                </div>
                <div style={{ fontSize: 11, fontWeight: 600, color: s.up ? "var(--si-brand-soft-tx)" : "#b91c1c", marginTop: 6 }}>
                  {s.delta}
                </div>
              </div>
            );
          })}
        </div>

        <SectionLabel className="!mt-4">Author activity</SectionLabel>
        <div className="flex flex-col" style={{ gap: 11 }}>
          {AUTHOR_ACTIVITY.map((a) => {
            const Icon = ACT_ICON[a.icon];
            return (
              <div key={a.id} className="flex gap-[11px]" style={{ marginBottom: 3 }}>
                <span
                  className="inline-flex items-center justify-center shrink-0"
                  style={{ width: 30, height: 30, borderRadius: 8, background: "#f1f2f4", color: a.iconColor }}
                >
                  <Icon size={14} color={a.iconColor as string} />
                </span>
                <div className="flex-1 min-w-0">
                  <div className="flex items-baseline justify-between gap-2">
                    <span style={{ fontSize: 13, fontWeight: 600 }}>{a.title}</span>
                    <span style={{ fontSize: 11, color: "var(--si-text-3)" }}>{a.date}</span>
                  </div>
                  <div style={{ fontSize: 12.5, color: "var(--si-text-2)", marginTop: 2 }}>{a.desc}</div>
                  <div className="flex flex-wrap items-center gap-2" style={{ fontSize: 11.5, color: "var(--si-text-3)", marginTop: 4 }}>
                    {"factText" in a && a.factText ? (
                      <span>
                        {a.factText} <strong style={{ color: "var(--si-text)" }}>{(a as any).factBold}</strong>
                      </span>
                    ) : null}
                    {a.factSentiment ? <SentimentDot type={a.factSentiment}>{a.factSentimentLabel}</SentimentDot> : null}
                    {"factSuffix" in a && (a as any).factSuffix ? <span className="tnum">{(a as any).factSuffix}</span> : null}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </aside>
  );
}
