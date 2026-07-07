import { useState } from "react";
import {
  Repeat2, AlertTriangle, Check, MessageCircle, Send, Heart, MessageSquare, Eye,
  Zap, Sparkles, Image as ImageIcon, Smile, BadgeCheck,
} from "lucide-react";
import { Avatar, PlatformBadge, Pill, SentimentDot, Button } from "./ui/primitives";

export function CenterColumn() {
  const [replyAs, setReplyAs] = useState<"public" | "dm">("public");
  const [text, setText] = useState(
    `Hi Jenna, your order shipped this morning and is now out for delivery — tracking #SA8842193. You'll have it by 6pm today. We've also added a credit to your account for the delay. 💛`,
  );
  const chars = text.length;
  const max = 280;
  const pct = Math.min(100, (chars / max) * 100);
  const warn = chars > 230;

  return (
    <section className="flex flex-col min-w-0 min-h-0 flex-1" style={{ background: "var(--si-panel-2)" }}>
      {/* Thread header */}
      <header style={{ background: "var(--si-panel)", borderBottom: "1px solid var(--si-border)", padding: "12px 18px" }}>
        <div className="flex items-center gap-3">
          <PlatformBadge platform="x" size="header" />
          <h1 className="truncate" style={{ fontSize: 15, fontWeight: 700, color: "var(--si-text)" }}>
            Public mention · @jenna_d
          </h1>
          <div className="ms-auto flex items-center gap-2">
            <Button size="sm" variant="ghost" icon={Repeat2}>Assign</Button>
            <Button size="sm" variant="ghost" danger icon={AlertTriangle}>Escalate</Button>
            <Button size="sm" variant="primary" icon={Check}>Resolve</Button>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2" style={{ marginTop: 9 }}>
          <SentimentDot type="negative">Negative sentiment</SentimentDot>
          <Pill variant="red">complaint</Pill>
          <span style={{ color: "var(--si-text-3)" }}>·</span>
          <span style={{ fontSize: 12, color: "var(--si-text-2)" }}>
            Reach: <strong style={{ color: "var(--si-text)" }}>12.4K followers</strong>
          </span>
          <span style={{ fontSize: 12, color: "var(--si-text-2)" }}>
            Assigned: <strong style={{ color: "var(--si-text)" }}>You</strong>
          </span>
        </div>
      </header>

      {/* SLA strip */}
      <div
        className="flex items-center gap-2"
        style={{
          padding: "8px 18px",
          background: "#fef2f2",
          borderBottom: "1px solid #fecdcd",
          color: "#b91c1c",
          fontSize: 12.5,
          fontWeight: 600,
        }}
      >
        <AlertTriangle size={14} style={{ color: "var(--si-danger)" }} />
        High-visibility complaint from a verified account — respond publicly within the 5-minute target.
      </div>

      {/* Thread scroll */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: 18 }}>
        {/* Post card */}
        <article
          style={{
            background: "var(--si-panel)", border: "1px solid var(--si-border)",
            borderRadius: 14, boxShadow: "var(--si-shadow-sm)", marginBottom: 14,
          }}
        >
          <div className="flex gap-[11px]" style={{ padding: "14px 16px 12px" }}>
            <Avatar initials="JD" color="rose" />
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-1.5">
                <span style={{ fontSize: 13.5, fontWeight: 600 }}>Jenna Doyle</span>
                <BadgeCheck size={14} style={{ color: "#1d9bf0", fill: "#1d9bf0" }} className="text-white" />
              </div>
              <div style={{ fontSize: 12.5, color: "var(--si-text-3)" }}>@jenna_d · 4 min ago</div>
            </div>
            <PlatformBadge platform="x" />
          </div>
          <p style={{ padding: "0 16px 14px", fontSize: 14, lineHeight: 1.6, color: "#1f2937" }}>
            .@YourBrand I ordered 2 weeks ago and still nothing — no tracking, no reply to my emails. This is honestly
            unacceptable for a "premium" service 😡 Order #80421
          </p>
          <div
            className="flex items-center gap-[22px] tnum"
            style={{
              borderTop: "1px solid var(--si-border)", padding: "11px 16px",
              fontSize: 12.5, fontWeight: 600, color: "var(--si-text-2)",
            }}
          >
            <span className="inline-flex items-center gap-1.5"><Heart size={14} style={{ color: "var(--si-text-3)" }} /> 47</span>
            <span className="inline-flex items-center gap-1.5"><MessageSquare size={14} style={{ color: "var(--si-text-3)" }} /> 12</span>
            <span className="inline-flex items-center gap-1.5"><Repeat2 size={14} style={{ color: "var(--si-text-3)" }} /> 23</span>
            <span className="inline-flex items-center gap-1.5"><Eye size={14} style={{ color: "var(--si-text-3)" }} /> 1.2K views</span>
          </div>
        </article>

        {/* Brand reply bubble */}
        <div style={{ paddingLeft: 46, marginBottom: 14 }}>
          <div
            style={{
              background: "#f0f7f1", border: "1px solid #d6ead9", borderRadius: 14,
              padding: "11px 14px", boxShadow: "var(--si-shadow-sm)", maxWidth: "90%",
            }}
          >
            <div className="flex items-center gap-[7px]">
              <Avatar initials="YB" color="green" size={22} />
              <span style={{ fontSize: 12.5, fontWeight: 600 }}>YourBrand</span>
              <Pill variant="green" style={{ height: 17, fontSize: 10, padding: "0 7px" }}>You · public</Pill>
              <span style={{ fontSize: 11.5, color: "var(--si-text-3)" }}>2 min ago</span>
            </div>
            <p style={{ fontSize: 13.5, lineHeight: 1.55, color: "#374151", marginTop: 6 }}>
              Hi Jenna — so sorry for the wait, that's not the experience we want. We've found order #80421 and a team
              member is on it now. Sending you a DM to sort this right away. 🙏
            </p>
            <div
              className="flex items-center"
              style={{ marginTop: 8, gap: 16, fontSize: 11.5, fontWeight: 600, color: "var(--si-text-3)" }}
            >
              <span>Posted publicly</span>
              <span className="inline-flex items-center gap-1"><Check size={12} /> Delivered</span>
            </div>
          </div>
        </div>

        {/* Customer follow-up */}
        <div className="flex gap-[11px]" style={{ paddingLeft: 6 }}>
          <Avatar initials="JD" color="rose" size={32} />
          <div
            style={{
              background: "var(--si-panel)", border: "1px solid var(--si-border)",
              borderRadius: 14, padding: "11px 14px", boxShadow: "var(--si-shadow-sm)", maxWidth: "90%",
            }}
          >
            <div className="flex items-center gap-2">
              <span style={{ fontSize: 12.5, fontWeight: 600 }}>Jenna Doyle</span>
              <span style={{ fontSize: 11.5, color: "var(--si-text-3)" }}>just now</span>
            </div>
            <p style={{ fontSize: 13.5, lineHeight: 1.55, marginTop: 6 }}>
              ok thank you — please just tell me where my package is 🙏
            </p>
            <div
              className="flex items-center"
              style={{ marginTop: 8, gap: 16, fontSize: 11.5, color: "var(--si-text-3)" }}
            >
              <button type="button" className="inline-flex items-center gap-1 transition-colors hover:text-[var(--si-text)]">
                <Heart size={12} /> Like
              </button>
              <button type="button" className="transition-colors hover:text-[var(--si-brand)]">Reply</button>
            </div>
          </div>
        </div>
      </div>

      {/* Composer */}
      <div style={{ background: "var(--si-panel)", borderTop: "1px solid var(--si-border)" }}>
        {/* Reply-as row */}
        <div
          className="flex items-center gap-2"
          style={{ padding: "8px 16px", borderBottom: "1px solid var(--si-border)" }}
        >
          <span style={{ fontSize: 12, fontWeight: 600, color: "var(--si-text-2)" }}>Reply as</span>
          {([
            { id: "public", label: "Public reply", Icon: MessageCircle },
            { id: "dm", label: "Direct message", Icon: Send },
          ] as const).map(({ id, label, Icon }) => {
            const active = replyAs === id;
            return (
              <button
                key={id}
                type="button"
                onClick={() => setReplyAs(id)}
                aria-pressed={active}
                className="inline-flex items-center gap-1.5 transition-colors"
                style={{
                  height: 28, padding: "0 11px", borderRadius: 999, fontSize: 12, fontWeight: 600,
                  background: active ? "var(--si-brand-soft)" : "#f1f2f4",
                  color: active ? "var(--si-brand-soft-tx)" : "var(--si-text-2)",
                }}
              >
                <Icon size={13} /> {label}
              </button>
            );
          })}
          <div className="ms-auto">
            <Pill variant="outline">
              {replyAs === "public" ? <>Posting to <strong style={{ marginInlineStart: 4, color: "var(--si-text)" }}>@YourBrand</strong></> : <>Sending to <strong style={{ marginInlineStart: 4, color: "var(--si-text)" }}>@jenna_d</strong></>}
            </Pill>
          </div>
        </div>

        {/* Textarea */}
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={replyAs === "public" ? "Write a public reply…" : "Write a direct message…"}
          className="w-full resize-none outline-none"
          style={{
            padding: "14px 16px", minHeight: 84, fontSize: 13.5, lineHeight: 1.6,
            color: "var(--si-text)", background: "transparent", border: 0,
          }}
        />

        {/* Foot toolbar */}
        <div
          className="flex items-center"
          style={{ padding: "8px 14px", borderTop: "1px solid var(--si-border)", gap: 4 }}
        >
          {[
            { Icon: Zap, label: "Canned" },
            { Icon: Sparkles, label: "AI suggest" },
            { Icon: ImageIcon, label: "Media" },
            { Icon: null, label: "GIF" },
          ].map(({ Icon, label }) => (
            <button
              key={label}
              type="button"
              className="inline-flex items-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)] hover:text-[var(--si-text)]"
              style={{
                height: 32, padding: "0 9px", borderRadius: 8, fontSize: 12.5, fontWeight: 500, color: "var(--si-text-2)",
              }}
            >
              {Icon ? <Icon size={15} /> : <span style={{ fontSize: 11, fontWeight: 700 }}>GIF</span>}
              {Icon ? label : null}
            </button>
          ))}
          <button
            type="button"
            aria-label="Emoji"
            className="inline-flex items-center justify-center transition-colors hover:bg-[var(--si-panel-2)] hover:text-[var(--si-text)]"
            style={{ width: 32, height: 32, borderRadius: 8, color: "var(--si-text-2)" }}
          >
            <Smile size={15} />
          </button>
          <div className="flex-1" />
          {/* Char counter ring */}
          <div className="inline-flex items-center gap-1.5">
            <span
              aria-hidden
              style={{
                width: 18, height: 18, borderRadius: 999,
                background: `conic-gradient(${warn ? "#b45309" : "var(--si-brand)"} ${pct * 3.6}deg, #e5e7eb 0deg)`,
                display: "inline-block",
              }}
            />
            <span className="tnum" style={{ fontSize: 12, fontWeight: 600, color: warn ? "#b45309" : "var(--si-text-3)" }}>
              {chars}
            </span>
          </div>
          <Button variant="primary" icon={Send} split className="ms-1">Reply</Button>
        </div>
      </div>
    </section>
  );
}
