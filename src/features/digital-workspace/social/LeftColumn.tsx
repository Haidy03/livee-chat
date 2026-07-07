import { Clock, RefreshCw, ChevronsLeft, Search, Filter, Check, Pencil } from "lucide-react";
import { useState } from "react";
import {
  Avatar, PlatformBadge, Pill, Tag, SentimentDot, IconButton, CountBadge, NumberBadge, SectionLabel,
  type Platform,
} from "./ui/primitives";
import { ENGAGE_ITEMS, SCHEDULED_ITEMS, DRAFT_ITEMS, PLATFORM_CHIPS, type EngageItem, type PublishItem } from "./data";
import { SiX, SiFacebook, SiInstagram } from "react-icons/si";
import { FaLinkedinIn } from "react-icons/fa6";

type Segment = "engage" | "publish";
type ChipId = (typeof PLATFORM_CHIPS)[number]["id"];

const CHIP_ICONS: Record<Exclude<ChipId, "all">, { Icon: any; color?: string }> = {
  x: { Icon: SiX, color: "#0f1419" },
  facebook: { Icon: SiFacebook, color: "#1877f2" },
  instagram: { Icon: SiInstagram, color: "#d6249f" },
  linkedin: { Icon: FaLinkedinIn, color: "#0a66c2" },
};

export function LeftColumn({
  selectedId, onSelect, unread,
}: {
  selectedId: string;
  onSelect: (id: string) => void;
  unread: Set<string>;
}) {
  const [segment, setSegment] = useState<Segment>("engage");
  const [activeChip, setActiveChip] = useState<ChipId>("all");
  const [search, setSearch] = useState("");

  return (
    <aside
      className="flex flex-col min-h-0"
      style={{ width: 326, background: "var(--si-panel)", borderRight: "1px solid var(--si-border)" }}
    >
      {/* Header */}
      <div className="flex items-center gap-2" style={{ padding: "14px 16px 10px" }}>
        <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--si-text)" }}>Social Inbox</h2>
        <CountBadge>23</CountBadge>
        <div className="flex-1" />
        <IconButton aria-label="Refresh"><RefreshCw size={17} /></IconButton>
        <IconButton aria-label="Collapse"><ChevronsLeft size={17} /></IconButton>
      </div>

      {/* Segmented */}
      <div style={{ margin: "0 16px 12px" }}>
        <div
          className="flex"
          style={{ background: "#f1f2f4", borderRadius: 9, padding: 3 }}
          role="tablist"
        >
          {(["engage", "publish"] as Segment[]).map((s) => {
            const active = segment === s;
            return (
              <button
                key={s}
                type="button"
                role="tab"
                aria-selected={active}
                onClick={() => setSegment(s)}
                className="flex-1 transition-colors capitalize"
                style={{
                  height: 32, borderRadius: 7, fontSize: 12.5, fontWeight: 600,
                  background: active ? "var(--si-panel)" : "transparent",
                  color: active ? "var(--si-text)" : "var(--si-text-2)",
                  boxShadow: active ? "var(--si-shadow-sm)" : undefined,
                }}
              >
                {s}
              </button>
            );
          })}
        </div>
      </div>

      {/* Search */}
      <div className="relative" style={{ margin: "0 16px 10px" }}>
        <Search
          size={16}
          style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)", color: "var(--si-text-3)" }}
        />
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search handle, mention, content…"
          className="w-full transition-colors"
          style={{
            height: 38, paddingLeft: 36, paddingRight: 12,
            border: "1px solid var(--si-border-2)", borderRadius: 10, fontSize: 13,
            background: "var(--si-panel)", color: "var(--si-text)",
          }}
          onFocus={(e) => (e.currentTarget.style.borderColor = "var(--si-brand)")}
          onBlur={(e) => (e.currentTarget.style.borderColor = "var(--si-border-2)")}
        />
      </div>

      {/* Filter bar */}
      <div className="flex gap-2" style={{ padding: "0 16px 10px" }}>
        {[
          { Icon: Filter, label: "Filter" },
          { Icon: Clock, label: "Highest priority" },
        ].map(({ Icon, label }) => (
          <button
            key={label}
            type="button"
            className="inline-flex items-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)]"
            style={{
              height: 30, padding: "0 10px", border: "1px solid var(--si-border-2)",
              borderRadius: 8, fontSize: 12.5, fontWeight: 600, color: "var(--si-text-2)", background: "var(--si-panel)",
            }}
          >
            <Icon size={13} /> {label}
          </button>
        ))}
      </div>

      {/* Platform chips */}
      <div className="flex gap-[7px] overflow-x-auto no-scrollbar" style={{ padding: "0 16px 12px" }}>
        {PLATFORM_CHIPS.map((c) => {
          const active = activeChip === c.id;
          const iconCfg = c.id !== "all" ? CHIP_ICONS[c.id as Exclude<ChipId, "all">] : null;
          return (
            <button
              key={c.id}
              type="button"
              onClick={() => setActiveChip(c.id)}
              className="inline-flex items-center gap-1.5 transition-colors shrink-0"
              style={{
                height: 30, padding: "0 12px", borderRadius: 999, fontSize: 12.5, fontWeight: 600,
                background: active ? "var(--si-brand)" : "#f1f2f4",
                color: active ? "#fff" : "var(--si-text-2)",
              }}
            >
              {iconCfg ? <iconCfg.Icon size={12} color={active ? "#fff" : iconCfg.color} /> : null}
              {c.label}
              <span style={{ fontWeight: 500, opacity: 0.85 }}>{c.count}</span>
            </button>
          );
        })}
      </div>

      {/* Lists */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: "2px 8px 12px" }}>
        {segment === "engage" ? (
          ENGAGE_ITEMS.map((it) => (
            <ListItem
              key={it.id}
              item={it}
              active={selectedId === it.id}
              unread={unread.has(it.id)}
              onClick={() => onSelect(it.id)}
            />
          ))
        ) : (
          <>
            <SectionLabel>Scheduled</SectionLabel>
            {SCHEDULED_ITEMS.map((it) => <PublishRow key={it.id} item={it} />)}
            <SectionLabel>Drafts</SectionLabel>
            {DRAFT_ITEMS.map((it) => <PublishRow key={it.id} item={it} />)}
          </>
        )}
      </div>
    </aside>
  );
}

function ListItem({
  item, active, unread, onClick,
}: {
  item: EngageItem;
  active: boolean;
  unread: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full text-left relative transition-colors"
      style={{
        display: "flex", gap: 11, padding: "12px 10px", borderRadius: 12,
        background: active ? "#eef7f0" : "transparent",
        boxShadow: active ? "inset 3px 0 0 var(--si-brand)" : undefined,
      }}
      onMouseEnter={(e) => { if (!active) e.currentTarget.style.background = "#f6f7f8"; }}
      onMouseLeave={(e) => { if (!active) e.currentTarget.style.background = "transparent"; }}
    >
      {unread && (
        <span
          aria-hidden
          style={{ position: "absolute", left: 3, top: 18, width: 7, height: 7, borderRadius: 999, background: "var(--si-brand)" }}
        />
      )}
      <div className="relative shrink-0">
        <Avatar initials={item.initials} color={item.avatarColor} />
        <div style={{ position: "absolute", right: -2, bottom: -2 }}>
          <PlatformBadge platform={item.platform} size="overlay" />
        </div>
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline justify-between gap-2">
          <span className="truncate" style={{ fontSize: 13.5, fontWeight: 600, color: "var(--si-text)" }}>
            {item.name}
          </span>
          <span className="tnum shrink-0" style={{ fontSize: 11.5, color: "var(--si-text-3)" }}>{item.time}</span>
        </div>
        <div className="truncate" style={{ fontSize: 12.5, color: "var(--si-text-2)" }}>{item.sub}</div>
        <div className="line-clamp-2" style={{ fontSize: 12.5, color: "var(--si-text)", marginTop: 2 }}>
          {item.preview}
        </div>
        <div className="flex flex-wrap items-center gap-[7px]" style={{ marginTop: 7 }}>
          <SentimentDot type={item.sentiment}>
            {item.sentiment === "positive" ? "Positive" : item.sentiment === "negative" ? "Negative" : "Neutral"}
          </SentimentDot>
          {item.tags?.map((t) => <Tag key={t.label} variant={t.variant}>{t.label}</Tag>)}
          {item.pills?.map((p) => (
            <Pill key={p.label} variant={p.variant} icon={p.icon === "check" ? Check : undefined}>
              {p.label}
            </Pill>
          ))}
          {item.sla && (
            <span
              className="inline-flex items-center gap-1 tnum"
              style={{ fontSize: 11.5, fontWeight: 600, color: item.sla.breached ? "#b91c1c" : "var(--si-brand-soft-tx)" }}
            >
              <Clock size={11} /> {item.sla.text}
            </span>
          )}
          {item.unreadCount ? (
            <span className="ms-auto"><NumberBadge>{item.unreadCount}</NumberBadge></span>
          ) : null}
        </div>
      </div>
    </button>
  );
}

function PublishRow({ item }: { item: PublishItem }) {
  const isDraft = item.platform === "draft";
  return (
    <div className="flex gap-[11px] transition-colors hover:bg-[#f6f7f8]" style={{ padding: "12px 10px", borderRadius: 12 }}>
      {isDraft ? (
        <span
          className="inline-flex items-center justify-center shrink-0"
          style={{ width: 40, height: 40, borderRadius: 9, background: "#94a3b8" }}
        >
          <Pencil size={18} color="#fff" />
        </span>
      ) : (
        <PlatformBadge platform={item.platform as Platform} size={40} />
      )}
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline justify-between gap-2">
          <span className="truncate" style={{ fontSize: 13.5, fontWeight: 600, color: "var(--si-text)" }}>{item.title}</span>
          <span className="shrink-0" style={{ fontSize: 11.5, fontWeight: 600, color: "var(--si-text-2)" }}>{item.time}</span>
        </div>
        <div className="line-clamp-2" style={{ fontSize: 12.5, color: "var(--si-text-2)", marginTop: 2 }}>
          {item.preview}
        </div>
        <div className="flex flex-wrap items-center gap-[7px]" style={{ marginTop: 7 }}>
          <Pill variant={item.pill.variant === "amber" ? "amber" : item.pill.variant === "outline" ? "outline" : "default"} icon={item.pill.icon === "clock" ? Clock : undefined}>
            {item.pill.label}
          </Pill>
          {item.tags.map((t) => <Tag key={t}>{t}</Tag>)}
        </div>
      </div>
    </div>
  );
}
