import { useEffect, useRef, useState } from "react";
import {
  Check, ChevronDown, Reply, MoreHorizontal, FileText, Clock, Send, RotateCcw, Download,
  Inbox, Star, Maximize2, X,
} from "lucide-react";
import { toast } from "sonner";
import { Avatar, Pill, IconButton, Button } from "../social/ui/primitives";
import {
  avatarColorOf, clearDraft, downloadAttachment, formatEmailTime, formatSnoozeUntil,
  hasDraft, initialsOf, loadDraft, saveDraft,
  useArchiveThread, useEmailMessages, useEmailSignature, useMarkThreadUnread,
  useReopenThread, useResolveThread, useSendReply, useSnoozeThread, useStarThread,
  type AttachmentUpload, type EmailMessage, type EmailThread,
} from "./api";
import {
  AttachmentPicker, DropMenu, HtmlEmailBody, RichComposer, TemplatePicker, snoozePresets,
  type RichComposerHandle,
} from "./components";

export function EmailThreadColumn({ thread }: { thread: EmailThread | null }) {
  const { data: messages = [], isLoading } = useEmailMessages(thread?.id ?? null);
  const resolve = useResolveThread();
  const reopen = useReopenThread();
  const archive = useArchiveThread();
  const markUnread = useMarkThreadUnread();
  const snooze = useSnoozeThread();
  const star = useStarThread();

  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [composerOpen, setComposerOpen] = useState(false);
  const [popOut, setPopOut] = useState(false);
  // Remount key: bumping re-seeds the inline composer from the saved draft
  // (e.g. after the pop-out modal closes or a reply is sent).
  const [epoch, setEpoch] = useState(0);

  // Latest message starts expanded whenever the thread tail changes.
  const latestId = messages.length > 0 ? messages[messages.length - 1].id : null;
  useEffect(() => {
    if (latestId) setExpanded((prev) => (prev.has(latestId) ? prev : new Set(prev).add(latestId)));
  }, [latestId]);

  // Switching conversations: composer opens only when an unsent draft exists.
  useEffect(() => {
    if (!thread?.id) return;
    setComposerOpen(hasDraft(thread.id));
    setPopOut(false);
    setEpoch((e) => e + 1);
  }, [thread?.id]);

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  if (!thread) {
    return (
      <main className="flex-1 flex flex-col items-center justify-center min-w-0 min-h-0 gap-3" style={{ background: "var(--si-panel-2)" }}>
        <span className="inline-flex items-center justify-center" style={{
          width: 56, height: 56, borderRadius: 999, background: "var(--si-panel)",
          border: "1px solid var(--si-border)", color: "var(--si-text-3)",
        }}>
          <Inbox size={24} />
        </span>
        <span style={{ fontSize: 13.5, color: "var(--si-text-3)" }}>Select a conversation, or press New to write one</span>
      </main>
    );
  }

  const snoozed = thread.snoozedUntil && new Date(thread.snoozedUntil) > new Date();

  return (
    <main className="flex-1 flex flex-col min-w-0 min-h-0" style={{ background: "var(--si-panel-2)" }}>
      {/* Header */}
      <div style={{ background: "var(--si-panel)", borderBottom: "1px solid var(--si-border)", padding: "12px 18px" }}>
        <div className="flex items-center gap-3">
          <h2 className="truncate" style={{ fontSize: 15, fontWeight: 700, color: "var(--si-text)" }}>
            {thread.subject}
          </h2>
          <div className="ms-auto flex items-center gap-2">
            <IconButton
              aria-label={thread.starred ? "Unstar" : "Star"}
              onClick={() => star.mutate({ threadId: thread.id, starred: !thread.starred })}
            >
              <Star size={16} style={thread.starred ? { color: "#f5b50a", fill: "#f5b50a" } : undefined} />
            </IconButton>
            <DropMenu
              trigger={(open) => (
                <Button variant="ghost" size="sm" icon={Clock} onClick={open}>
                  {snoozed ? "Snoozed" : "Snooze"}
                </Button>
              )}
              items={[
                ...snoozePresets().map((p) => ({
                  label: p.label,
                  onClick: () => snooze.mutate({ threadId: thread.id, until: p.until }),
                })),
                ...(snoozed
                  ? [{ label: "Unsnooze", onClick: () => snooze.mutate({ threadId: thread.id, until: null }) }]
                  : []),
              ]}
            />
            {thread.status === "open" ? (
              <Button variant="primary" size="sm" icon={Check} onClick={() => resolve.mutate(thread.id)}>
                Resolve
              </Button>
            ) : (
              <Button variant="primary" size="sm" icon={RotateCcw} onClick={() => reopen.mutate(thread.id)}>
                {thread.status === "archived" ? "Unarchive" : "Reopen"}
              </Button>
            )}
            <DropMenu
              trigger={(open) => <IconButton aria-label="More" onClick={open}><MoreHorizontal size={16} /></IconButton>}
              items={[
                ...(thread.status !== "archived"
                  ? [{ label: "Archive", onClick: () => archive.mutate(thread.id) }]
                  : []),
                { label: "Mark as unread", onClick: () => markUnread.mutate(thread.id) },
              ]}
            />
          </div>
        </div>
        <div className="flex items-center flex-wrap gap-2" style={{ marginTop: 9 }}>
          {thread.status === "resolved" && <Pill variant="green" icon={Check}>Resolved</Pill>}
          {thread.status === "archived" && <Pill variant="default">Archived</Pill>}
          {thread.status === "open" && !snoozed && <Pill variant="default">Open</Pill>}
          {snoozed && <Pill variant="default" icon={Clock}>Snoozed until {formatSnoozeUntil(thread.snoozedUntil!)}</Pill>}
          <span style={{ color: "var(--si-text-3)" }}>·</span>
          <span style={{ fontSize: 12, color: "var(--si-text-2)" }}>
            From: <b style={{ color: "var(--si-text)" }}>{thread.counterpartEmail}</b>
          </span>
          <span style={{ fontSize: 12, color: "var(--si-text-2)" }}>
            Via: <b style={{ color: "var(--si-text)" }}>{thread.mailbox}</b>
          </span>
        </div>
      </div>

      {/* Scroll area — centered reading pane */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: 18 }}>
        <div style={{ maxWidth: 860, margin: "0 auto" }}>
          {isLoading && messages.length === 0 && (
            <div style={{ padding: 24, textAlign: "center", fontSize: 13, color: "var(--si-text-3)" }}>Loading…</div>
          )}
          {messages.map((msg, i) => (
            <EmailCard
              key={msg.id}
              msg={msg}
              open={expanded.has(msg.id)}
              onToggle={() => toggle(msg.id)}
              latest={i === messages.length - 1}
            />
          ))}
        </div>
      </div>

      {/* Reply area */}
      <div style={{ padding: "0 18px 16px", background: "var(--si-panel-2)" }}>
        <div style={{ maxWidth: 860, margin: "0 auto" }}>
          {!composerOpen ? (
            /* Collapsed: slim Gmail-style reply bar */
            <div className="flex items-center gap-2" style={{
              background: "var(--si-panel)", border: "1px solid var(--si-border)",
              borderRadius: 12, padding: "8px 12px",
            }}>
              <Button variant="ghost" size="sm" icon={Reply} onClick={() => setComposerOpen(true)}>
                Reply to {thread.counterpartName.split(" ")[0]}
              </Button>
              <div className="flex-1" />
              <IconButton aria-label="Reply in window" onClick={() => { setComposerOpen(true); setPopOut(true); }}>
                <Maximize2 size={15} />
              </IconButton>
            </div>
          ) : !popOut ? (
            <div style={{
              background: "var(--si-panel)", border: "1px solid var(--si-border)",
              borderRadius: 14, boxShadow: "0 4px 16px rgba(15, 23, 42, .07)", overflow: "hidden",
            }}>
              <ReplyComposer
                key={`inline-${thread.id}-${epoch}`}
                thread={thread}
                onPopOut={() => setPopOut(true)}
                onHide={() => { setComposerOpen(false); setEpoch((e) => e + 1); }}
                onSent={() => { setComposerOpen(false); setEpoch((e) => e + 1); }}
              />
            </div>
          ) : null}
        </div>
      </div>

      {/* Pop-out reply modal */}
      {popOut && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center"
          style={{ background: "rgba(15, 23, 42, .45)" }}
          onMouseDown={(e) => { if (e.target === e.currentTarget) { setPopOut(false); setEpoch((v) => v + 1); } }}
        >
          <div
            className="flex flex-col"
            style={{
              width: "min(820px, calc(100vw - 32px))",
              maxHeight: "calc(100vh - 48px)",
              background: "var(--si-panel)",
              borderRadius: 14,
              border: "1px solid var(--si-border)",
              boxShadow: "0 24px 64px rgba(15, 23, 42, .25)",
              overflow: "hidden",
            }}
          >
            <div className="flex items-center gap-2" style={{ padding: "12px 16px", borderBottom: "1px solid var(--si-border)" }}>
              <Reply size={15} style={{ color: "var(--si-text-3)" }} />
              <h3 className="truncate" style={{ fontSize: 14, fontWeight: 700, color: "var(--si-text)" }}>
                Re: {thread.subject}
              </h3>
              <div className="flex-1" />
              <button
                type="button"
                aria-label="Close"
                onClick={() => { setPopOut(false); setEpoch((v) => v + 1); }}
                className="inline-flex items-center justify-center transition-colors hover:bg-[var(--si-panel-2)]"
                style={{ width: 30, height: 30, borderRadius: 7, color: "var(--si-text-2)" }}
              >
                <X size={16} />
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-y-auto">
              <ReplyComposer
                key={`modal-${thread.id}`}
                thread={thread}
                large
                onSent={() => { setPopOut(false); setComposerOpen(false); setEpoch((v) => v + 1); }}
              />
            </div>
          </div>
        </div>
      )}
    </main>
  );
}

/* -------------------- ReplyComposer -------------------- */

/**
 * The reply editor used both inline (bottom card) and in the pop-out modal.
 * Draft state lives in localStorage keyed by thread, so the two stay in sync:
 * each instance seeds itself from the draft (or the signature) on mount.
 */
function ReplyComposer({ thread, large, onPopOut, onHide, onSent }: {
  thread: EmailThread;
  large?: boolean;
  onPopOut?: () => void;
  onHide?: () => void;
  onSent?: () => void;
}) {
  const sendReply = useSendReply(thread.id);
  const { data: signature } = useEmailSignature();

  const editor = useRef<RichComposerHandle>(null);
  // What the editor was seeded with (signature) — identical content is not a draft.
  const seededHtml = useRef<string>("");
  const seededOnce = useRef(false);
  const [hasBody, setHasBody] = useState(false);
  const [cc, setCc] = useState("");
  const [showCc, setShowCc] = useState(false);
  const [attachments, setAttachments] = useState<AttachmentUpload[]>([]);

  // Seed on mount: saved draft first, else signature. Component is remounted
  // (keyed) per thread/surface, so this runs exactly once per appearance.
  useEffect(() => {
    if (seededOnce.current) return;
    const draft = loadDraft(thread.id);
    if (draft !== null) {
      editor.current?.setHtml(draft);
      seededHtml.current = "";
      seededOnce.current = true;
    } else if (signature !== undefined) {
      if (signature.html) {
        editor.current?.setHtml(`<br/><br/>${signature.html}`);
        seededHtml.current = editor.current?.getHtml() ?? "";
        clearDraft(thread.id); // setHtml's change event fires before seededHtml is set
      }
      seededOnce.current = true;
    }
  }, [thread.id, signature]);

  const onEditorChange = (text: string) => {
    setHasBody(text.length > 0);
    const html = editor.current?.getHtml() ?? "";
    if (html === seededHtml.current) clearDraft(thread.id);
    else saveDraft(thread.id, html, text);
  };

  const send = () => {
    const body = editor.current?.getText().trim() ?? "";
    if (!body || sendReply.isPending) return;
    sendReply.mutate(
      {
        body,
        htmlBody: editor.current?.getHtml(),
        cc: cc.split(",").map((s) => s.trim()).filter((s) => s.includes("@")),
        attachments,
      },
      {
        onSuccess: () => {
          clearDraft(thread.id);
          onSent?.();
        },
        onError: (err) => toast.error(err instanceof Error ? err.message : String(err)),
      },
    );
  };

  return (
    <div>
      {/* Recipient row */}
      <div className="flex items-center gap-2" style={{ padding: "9px 12px 9px 16px" }}>
        <span className="inline-flex items-center truncate" style={{
          height: 24, padding: "0 8px", borderRadius: 7, maxWidth: "55%",
          background: "#eef2f7", fontSize: 12, fontWeight: 500, color: "#334155",
        }}>
          <span className="truncate">{thread.counterpartName} &lt;{thread.counterpartEmail}&gt;</span>
        </span>
        {!showCc && (
          <button
            type="button"
            onClick={() => setShowCc(true)}
            style={{ fontSize: 12, fontWeight: 600, color: "var(--si-brand-soft-tx)" }}
          >
            Cc
          </button>
        )}
        <div className="flex-1" />
        {onPopOut && (
          <IconButton aria-label="Open in window" onClick={onPopOut}><Maximize2 size={14} /></IconButton>
        )}
        {onHide && (
          <IconButton aria-label="Hide reply" onClick={onHide}><X size={15} /></IconButton>
        )}
      </div>
      {showCc && (
        <div className="flex items-center gap-2.5" style={{ padding: "0 16px 8px" }}>
          <span style={{ width: 24, fontSize: 12, fontWeight: 600, color: "var(--si-text-3)" }}>Cc</span>
          <input
            value={cc}
            onChange={(e) => setCc(e.target.value)}
            placeholder="comma, separated, addresses"
            className="flex-1 outline-none bg-transparent"
            style={{ fontSize: 13, color: "var(--si-text)" }}
          />
        </div>
      )}

      <div style={{ borderTop: "1px solid var(--si-border)" }}>
        <RichComposer
          ref={editor}
          placeholder="Write your reply…"
          minHeight={large ? 260 : 96}
          onTextChange={onEditorChange}
        />
      </div>

      {/* Footer */}
      <div className="flex items-center flex-wrap" style={{ padding: "8px 14px", borderTop: "1px solid var(--si-border)", gap: 4 }}>
        <TemplatePicker onInsert={(text) => editor.current?.insertText(text)} />
        <AttachmentPicker attachments={attachments} onChange={setAttachments} />
        <span style={{ fontSize: 11.5, color: "var(--si-text-3)" }}>
          as <b style={{ color: "var(--si-text-2)" }}>{thread.mailbox}</b>
        </span>
        <div className="flex-1" />
        <Button variant="primary" size="md" icon={Send} onClick={send} disabled={!hasBody || sendReply.isPending}>
          {sendReply.isPending ? "Sending…" : "Send"}
        </Button>
      </div>
    </div>
  );
}

/* -------------------- EmailCard -------------------- */

function EmailCard({
  msg, open, onToggle, latest,
}: {
  msg: EmailMessage; open: boolean; onToggle: () => void; latest?: boolean;
}) {
  const outbound = msg.direction === "outbound";
  const displayName = outbound ? (msg.agentName ?? msg.fromName) : msg.fromName;

  return (
    <article
      style={{
        background: "var(--si-panel)",
        border: "1px solid var(--si-border)",
        borderInlineStart: outbound ? "3px solid var(--si-brand)" : "1px solid var(--si-border)",
        borderRadius: 14,
        boxShadow: "var(--si-shadow-sm)",
        marginBottom: 14,
        overflow: "hidden",
      }}
    >
      {/* Header */}
      <div
        role="button"
        tabIndex={0}
        onClick={onToggle}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onToggle(); } }}
        className="flex items-start gap-3 cursor-pointer transition-colors hover:bg-[var(--si-panel-2)]"
        style={{ padding: "14px 16px" }}
      >
        <Avatar
          initials={initialsOf(displayName)}
          color={outbound ? "green" : avatarColorOf(msg.fromEmail)}
          size="sm"
        />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span style={{ fontSize: 13.5, fontWeight: 600, color: "var(--si-text)" }}>{displayName}</span>
            {!outbound && (
              <span style={{ fontSize: 12, color: "var(--si-text-3)" }}>&lt;{msg.fromEmail}&gt;</span>
            )}
            {outbound && (
              <span className="inline-flex items-center" style={{
                height: 18, padding: "0 7px", borderRadius: 999,
                background: "var(--si-brand-soft)", color: "var(--si-brand-soft-tx)",
                fontSize: 10, fontWeight: 700,
              }}>You</span>
            )}
          </div>
          <div style={{ fontSize: 12, color: "var(--si-text-2)", marginTop: 2 }}>
            to {msg.toEmail}
            {msg.ccEmails.length > 0 && <> · cc {msg.ccEmails.join(", ")}</>}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          <span className="tnum" style={{ fontSize: 12, color: "var(--si-text-3)" }}>{formatEmailTime(msg.sentAt)}</span>
          <ChevronDown size={16} style={{ color: "var(--si-text-3)", transform: open ? "rotate(180deg)" : undefined, transition: "transform .14s" }} />
        </div>
      </div>

      {open && (
        <>
          <div style={{ padding: "4px 18px 16px" }}>
            {msg.htmlBody ? (
              <HtmlEmailBody html={msg.htmlBody} />
            ) : (
              <div style={{ fontSize: 13.5, lineHeight: 1.65, color: "#374151", whiteSpace: "pre-wrap", overflowWrap: "anywhere" }}>
                {msg.textBody || "(no content)"}
              </div>
            )}
          </div>
          {msg.attachmentNames.length > 0 && (
            <div className="flex flex-wrap gap-2.5" style={{ padding: "0 18px 16px" }}>
              {msg.attachmentNames.map((name, i) => (
                <AttachmentCard key={i} name={name} messageId={msg.id} index={i} inbound={!outbound} />
              ))}
            </div>
          )}
        </>
      )}
    </article>
  );
}

function AttachmentCard({ name, messageId, index, inbound }: {
  name: string; messageId: string; index: number; inbound: boolean;
}) {
  const [busy, setBusy] = useState(false);

  const download = async () => {
    if (!inbound || busy) return;
    setBusy(true);
    try {
      await downloadAttachment(messageId, index, name);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <button
      type="button"
      onClick={download}
      disabled={!inbound || busy}
      title={inbound ? "Download" : "Sent attachment"}
      className="inline-flex items-center transition-colors enabled:hover:bg-[#eef2f7] enabled:cursor-pointer"
      style={{
        gap: 9, height: 44, padding: "0 13px 0 11px", textAlign: "start",
        border: "1px solid var(--si-border-2)", borderRadius: 10, background: "var(--si-panel-2)",
        opacity: busy ? 0.6 : 1,
      }}
    >
      <span className="inline-flex items-center justify-center" style={{
        width: 30, height: 30, borderRadius: 7, background: "#eef2f7", color: "var(--si-a-blue)",
      }}>
        <FileText size={15} />
      </span>
      <div>
        <div style={{ fontSize: 12.5, fontWeight: 600, lineHeight: 1.2, color: "var(--si-text)" }}>{name}</div>
        <div style={{ fontSize: 11, color: "var(--si-text-3)" }}>{busy ? "downloading…" : "attachment"}</div>
      </div>
      {inbound && <Download size={14} style={{ color: "var(--si-text-3)", marginLeft: 4 }} />}
    </button>
  );
}
