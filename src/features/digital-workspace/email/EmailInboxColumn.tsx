import { useEffect, useMemo, useState } from "react";
import {
  RefreshCw, Search, Filter, ArrowDownUp, Paperclip, CornerUpLeft, Check, CheckCheck, Clock, SquarePen, Archive, Inbox, Send, Settings2, Star,
} from "lucide-react";
import { Avatar, Button, IconButton, CountBadge, NumberBadge, Pill } from "../social/ui/primitives";
import {
  avatarColorOf, folderOf, formatEmailTime, hasDraft, initialsOf, threadsInFolder,
  useEmailMailboxes, useStarThread,
  type EmailFolder, type EmailThread,
} from "./api";
import { DropMenu } from "./components";
import { SignatureDialog } from "./SignatureDialog";

type RowFilter = "all" | "unread" | "read" | "starred" | "attachments" | "drafts";

const FOLDERS: { id: EmailFolder; label: string; icon: any }[] = [
  { id: "inbox", label: "Inbox", icon: Inbox },
  { id: "snoozed", label: "Snoozed", icon: Clock },
  { id: "sent", label: "Sent", icon: Send },
  { id: "archived", label: "Archived", icon: Archive },
  { id: "resolved", label: "Resolved", icon: CheckCheck },
];

const FILTER_LABEL: Record<RowFilter, string> = {
  all: "Filter",
  unread: "Unread",
  read: "Read",
  starred: "Starred",
  attachments: "Attachments",
  drafts: "Drafts",
};

export function EmailInboxColumn({
  threads, loading, selectedId, onSelect, onRefresh, onCompose, onVisibleChange,
}: {
  threads: EmailThread[];
  loading: boolean;
  selectedId: string | null;
  onSelect: (id: string) => void;
  onRefresh: () => void;
  onCompose: () => void;
  /** Visible (filtered+sorted) thread ids — drives j/k keyboard navigation in the parent. */
  onVisibleChange?: (ids: string[]) => void;
}) {
  const { data: mailboxes = [] } = useEmailMailboxes();
  const [folder, setFolder] = useState<EmailFolder>("inbox");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<RowFilter>("all");
  const [newestFirst, setNewestFirst] = useState(true);
  const [mailbox, setMailbox] = useState<string>("");
  const [editingSignature, setEditingSignature] = useState(false);

  const scoped = useMemo(
    () => (mailbox ? threads.filter((t) => t.mailbox === mailbox) : threads),
    [threads, mailbox],
  );

  const visible = useMemo(() => {
    let list = threadsInFolder(scoped, folder);

    if (filter === "unread") list = list.filter((t) => t.unreadCount > 0);
    if (filter === "read") list = list.filter((t) => t.unreadCount === 0);
    if (filter === "starred") list = list.filter((t) => t.starred);
    if (filter === "attachments") list = list.filter((t) => t.lastMessageHasAttachments);
    if (filter === "drafts") list = list.filter((t) => hasDraft(t.id));

    const q = search.trim().toLowerCase();
    if (q) {
      list = list.filter((t) =>
        [t.counterpartName, t.counterpartEmail, t.subject, t.lastMessageSnippet]
          .some((s) => s.toLowerCase().includes(q)),
      );
    }

    return [...list].sort((a, b) => {
      const d = new Date(a.lastMessageAt).getTime() - new Date(b.lastMessageAt).getTime();
      return newestFirst ? -d : d;
    });
  }, [scoped, folder, filter, search, newestFirst]);

  const inboxCount = threadsInFolder(scoped, "inbox").length;
  const countOf = (f: EmailFolder) => threadsInFolder(scoped, f).length;

  useEffect(() => {
    onVisibleChange?.(visible.map((t) => t.id));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [visible]);

  return (
    <aside
      className="flex flex-col min-h-0 max-[1180px]:w-[290px] max-[900px]:w-full"
      style={{ width: 326, background: "var(--si-panel)", borderRight: "1px solid var(--si-border)" }}
    >
      {/* Header */}
      <div className="flex items-center gap-2" style={{ padding: "14px 16px 10px" }}>
        <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--si-text)" }}>Email</h2>
        <CountBadge>{inboxCount}</CountBadge>
        <div className="flex-1" />
        <Button variant="primary" size="sm" icon={SquarePen} onClick={onCompose}>New</Button>
        <IconButton aria-label="Refresh" onClick={onRefresh}><RefreshCw size={17} /></IconButton>
        <IconButton aria-label="Signature settings" onClick={() => setEditingSignature(true)}><Settings2 size={17} /></IconButton>
      </div>
      {editingSignature && <SignatureDialog onClose={() => setEditingSignature(false)} />}

      {/* Mailbox picker (only when several accounts are configured) */}
      {mailboxes.length > 1 && (
        <div style={{ margin: "0 16px 10px" }}>
          <select
            value={mailbox}
            onChange={(e) => setMailbox(e.target.value)}
            className="w-full outline-none"
            style={{
              height: 34, border: "1px solid var(--si-border-2)", borderRadius: 10,
              padding: "0 10px", fontSize: 12.5, background: "var(--si-panel)", color: "var(--si-text)",
            }}
          >
            <option value="">All mailboxes</option>
            {mailboxes.map((m) => (
              <option key={m.address} value={m.address}>{m.displayName} &lt;{m.address}&gt;</option>
            ))}
          </select>
        </div>
      )}

      {/* Search */}
      <div className="relative" style={{ margin: "0 16px 10px" }}>
        <Search size={16} className="absolute" style={{ left: 12, top: "50%", transform: "translateY(-50%)", color: "var(--si-text-3)" }} />
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search subject, sender, content…"
          className="w-full outline-none"
          style={{
            height: 38, border: "1px solid var(--si-border-2)", borderRadius: 10,
            padding: "0 12px 0 36px", fontSize: 13, background: "var(--si-panel)", color: "var(--si-text)",
          }}
          onFocus={(e) => { e.currentTarget.style.borderColor = "var(--si-brand)"; e.currentTarget.style.boxShadow = "0 0 0 3px var(--si-ring)"; }}
          onBlur={(e) => { e.currentTarget.style.borderColor = "var(--si-border-2)"; e.currentTarget.style.boxShadow = "none"; }}
        />
      </div>

      {/* Filter bar */}
      <div className="flex gap-2" style={{ padding: "0 16px 10px" }}>
        <DropMenu
          align="start"
          trigger={(open) => (
            <FilterButton icon={Filter} active={filter !== "all"} onClick={open}>
              {FILTER_LABEL[filter]}
            </FilterButton>
          )}
          items={[
            { label: "All conversations", onClick: () => setFilter("all") },
            { label: "Unread only", onClick: () => setFilter("unread") },
            { label: "Read only", onClick: () => setFilter("read") },
            { label: "Starred", onClick: () => setFilter("starred") },
            { label: "With attachments", onClick: () => setFilter("attachments") },
            { label: "Has draft reply", onClick: () => setFilter("drafts") },
          ]}
        />
        <FilterButton icon={ArrowDownUp} onClick={() => setNewestFirst((v) => !v)}>
          {newestFirst ? "Newest activity" : "Oldest activity"}
        </FilterButton>
      </div>

      {/* Folder chips — wrap so every folder stays visible */}
      <div className="flex flex-wrap" style={{ padding: "0 16px 12px", gap: 7 }}>
        {FOLDERS.map((f) => {
          const active = folder === f.id;
          const count = countOf(f.id);
          const Icon = f.icon;
          return (
            <button
              key={f.id}
              type="button"
              onClick={() => setFolder(f.id)}
              className="shrink-0 inline-flex items-center gap-1.5 transition-colors"
              style={{
                height: 30, padding: "0 12px", borderRadius: 999, fontSize: 12.5, fontWeight: 600,
                background: active ? "var(--si-brand)" : "#f1f2f4",
                color: active ? "#fff" : "var(--si-text-2)",
              }}
            >
              <Icon size={13} style={{ opacity: active ? 0.9 : 0.65 }} />
              {f.label}
              {count > 0 && <span style={{ opacity: active ? 0.85 : 0.7, fontWeight: 500 }}>{count}</span>}
            </button>
          );
        })}
      </div>

      {/* List */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: "2px 8px 12px" }}>
        {loading && threads.length === 0 && (
          <div style={{ padding: "24px 12px", fontSize: 13, color: "var(--si-text-3)", textAlign: "center" }}>
            Loading…
          </div>
        )}
        {!loading && visible.length === 0 && (
          <div style={{ padding: "24px 12px", fontSize: 13, color: "var(--si-text-3)", textAlign: "center" }}>
            No conversations here.
          </div>
        )}
        {visible.map((t) => (
          <ThreadRow key={t.id} thread={t} active={selectedId === t.id} onClick={() => onSelect(t.id)} />
        ))}
      </div>
    </aside>
  );
}

function FilterButton({ icon: Icon, children, onClick, active }: {
  icon: any; children: React.ReactNode; onClick?: () => void; active?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)]"
      style={{
        height: 30, padding: "0 11px", borderRadius: 8, fontSize: 12.5, fontWeight: 600,
        color: active ? "var(--si-brand-soft-tx)" : "var(--si-text-2)",
        border: `1px solid ${active ? "var(--si-brand)" : "var(--si-border-2)"}`,
        background: "var(--si-panel)",
      }}
    >
      <Icon size={14} />
      {children}
    </button>
  );
}

function ThreadRow({ thread, active, onClick }: {
  thread: EmailThread; active: boolean; onClick: () => void;
}) {
  const unread = thread.unreadCount > 0;
  const state = folderOf(thread);
  const star = useStarThread();
  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onClick(); } }}
      className="relative flex cursor-pointer transition-colors"
      style={{
        gap: 11, padding: "12px 10px", borderRadius: 12,
        background: active ? "#eef7f0" : undefined,
        boxShadow: active ? "inset 3px 0 0 var(--si-brand)" : undefined,
      }}
      onMouseEnter={(e) => { if (!active) e.currentTarget.style.background = "#f6f7f8"; }}
      onMouseLeave={(e) => { if (!active) e.currentTarget.style.background = ""; }}
    >
      {unread && (
        <span
          aria-hidden
          style={{ position: "absolute", left: 3, top: 18, width: 7, height: 7, borderRadius: 999, background: "var(--si-dot)" }}
        />
      )}
      <Avatar initials={initialsOf(thread.counterpartName)} color={avatarColorOf(thread.counterpartEmail)} />
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline justify-between gap-2">
          <span className="truncate" style={{ fontSize: 13.5, fontWeight: 600, color: "var(--si-text)" }}>
            {thread.counterpartName}
          </span>
          <span className="flex items-center gap-1 shrink-0">
            <button
              type="button"
              aria-label={thread.starred ? "Unstar" : "Star"}
              onClick={(e) => {
                e.stopPropagation();
                star.mutate({ threadId: thread.id, starred: !thread.starred });
              }}
              className="inline-flex items-center justify-center transition-colors hover:bg-[#eef2f7]"
              style={{ width: 20, height: 20, borderRadius: 5 }}
            >
              <Star
                size={13}
                style={thread.starred
                  ? { color: "#f5b50a", fill: "#f5b50a" }
                  : { color: "var(--si-text-3)" }}
              />
            </button>
            <span className="tnum" style={{ fontSize: 11.5, color: "var(--si-text-3)" }}>
              {formatEmailTime(thread.lastMessageAt)}
            </span>
          </span>
        </div>
        <div className="truncate" style={{ fontSize: 12.5, fontWeight: unread ? 700 : 500, color: unread ? "var(--si-text)" : "var(--si-text-2)", marginTop: 1 }}>
          {thread.subject}
        </div>
        <div className="flex items-center gap-1.5 truncate" style={{ fontSize: 12.5, color: "var(--si-text-2)", marginTop: 1 }}>
          {thread.lastMessageHasAttachments && <Paperclip size={13} style={{ color: "var(--si-text-3)", flexShrink: 0 }} />}
          {thread.lastMessageDirection === "outbound" && <CornerUpLeft size={13} style={{ color: "var(--si-text-3)", flexShrink: 0 }} />}
          <span className="truncate">{thread.lastMessageSnippet}</span>
        </div>
        <div className="flex items-center flex-wrap gap-1.5" style={{ marginTop: 7 }}>
          {state === "resolved" && <Pill variant="green" icon={Check}>Resolved</Pill>}
          {state === "archived" && <Pill variant="default" icon={Archive}>Archived</Pill>}
          {state === "snoozed" && <Pill variant="default" icon={Clock}>Snoozed</Pill>}
          {hasDraft(thread.id) && (
            <span style={{ fontSize: 11.5, fontWeight: 600, color: "#b45309" }}>Draft</span>
          )}
          <span className="ms-auto"><NumberBadge>{thread.messageCount}</NumberBadge></span>
        </div>
      </div>
    </div>
  );
}
