import { useEffect, useRef, useState } from "react";
import { X, Send } from "lucide-react";
import { toast } from "sonner";
import { Button } from "../social/ui/primitives";
import { AttachmentPicker, RichComposer, TemplatePicker, type RichComposerHandle } from "./components";
import { useComposeEmail, useEmailMailboxes, useEmailSignature, type AttachmentUpload } from "./api";

export function ComposeDialog({ onClose, onSent, initialTo, initialToName }: {
  onClose: () => void;
  onSent: (threadId: string) => void;
  initialTo?: string;
  initialToName?: string;
}) {
  const { data: mailboxes = [] } = useEmailMailboxes();
  const { data: signature } = useEmailSignature();
  const compose = useComposeEmail();
  const editor = useRef<RichComposerHandle>(null);

  const [mailbox, setMailbox] = useState<string>("");
  const [to, setTo] = useState(initialTo ?? "");
  const [cc, setCc] = useState("");
  const [showCc, setShowCc] = useState(false);
  const [subject, setSubject] = useState("");
  const [hasBody, setHasBody] = useState(false);
  const [attachments, setAttachments] = useState<AttachmentUpload[]>([]);
  const [seeded, setSeeded] = useState(false);

  // Seed the signature once, only while the body is still untouched.
  useEffect(() => {
    if (seeded || hasBody || !signature?.html) return;
    editor.current?.setHtml(`<br/><br/>${signature.html}`);
    setSeeded(true);
  }, [signature, seeded, hasBody]);

  const canSend = to.trim().includes("@") && subject.trim().length > 0 && hasBody && !compose.isPending;

  const send = () => {
    if (!canSend) return;
    compose.mutate(
      {
        mailbox: mailbox || undefined,
        to: to.trim(),
        toName: initialToName,
        cc: cc.split(",").map((s) => s.trim()).filter((s) => s.includes("@")),
        subject: subject.trim(),
        body: editor.current?.getText() ?? "",
        htmlBody: editor.current?.getHtml(),
        attachments,
      },
      {
        onSuccess: (thread) => {
          toast.success("Email sent");
          onSent(thread.id);
          onClose();
        },
        onError: (err) => toast.error(err instanceof Error ? err.message : String(err)),
      },
    );
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      style={{ background: "rgba(15, 23, 42, .45)" }}
      onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div
        className="flex flex-col"
        style={{
          width: "min(680px, calc(100vw - 32px))",
          maxHeight: "calc(100vh - 64px)",
          background: "var(--si-panel)",
          borderRadius: 14,
          border: "1px solid var(--si-border)",
          boxShadow: "0 24px 64px rgba(15, 23, 42, .25)",
          overflow: "hidden",
        }}
      >
        {/* Title bar */}
        <div className="flex items-center" style={{ padding: "12px 16px", borderBottom: "1px solid var(--si-border)" }}>
          <h3 style={{ fontSize: 14.5, fontWeight: 700, color: "var(--si-text)" }}>New email</h3>
          <div className="flex-1" />
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="inline-flex items-center justify-center transition-colors hover:bg-[var(--si-panel-2)]"
            style={{ width: 30, height: 30, borderRadius: 7, color: "var(--si-text-2)" }}
          >
            <X size={16} />
          </button>
        </div>

        {/* Fields */}
        <div style={{ padding: "4px 16px 0" }}>
          {mailboxes.length > 1 && (
            <ComposeField label="From">
              <select
                value={mailbox}
                onChange={(e) => setMailbox(e.target.value)}
                className="flex-1 outline-none bg-transparent"
                style={{ fontSize: 13, color: "var(--si-text)" }}
              >
                {mailboxes.map((m) => (
                  <option key={m.address} value={m.address}>
                    {m.displayName} &lt;{m.address}&gt;
                  </option>
                ))}
              </select>
            </ComposeField>
          )}
          <ComposeField label="To">
            <input
              value={to}
              onChange={(e) => setTo(e.target.value)}
              placeholder="customer@example.com"
              className="flex-1 outline-none bg-transparent"
              style={{ fontSize: 13, color: "var(--si-text)" }}
              autoFocus
            />
            {!showCc && (
              <button
                type="button"
                onClick={() => setShowCc(true)}
                style={{ fontSize: 12, fontWeight: 600, color: "var(--si-brand-soft-tx)" }}
              >
                Cc
              </button>
            )}
          </ComposeField>
          {showCc && (
            <ComposeField label="Cc">
              <input
                value={cc}
                onChange={(e) => setCc(e.target.value)}
                placeholder="comma, separated, addresses"
                className="flex-1 outline-none bg-transparent"
                style={{ fontSize: 13, color: "var(--si-text)" }}
              />
            </ComposeField>
          )}
          <ComposeField label="Subject" noBorder>
            <input
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="flex-1 outline-none bg-transparent"
              style={{ fontSize: 13, color: "var(--si-text)" }}
            />
          </ComposeField>
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 overflow-y-auto" style={{ borderTop: "1px solid var(--si-border)" }}>
          <RichComposer ref={editor} minHeight={180} onTextChange={(t) => setHasBody(t.length > 0)} />
        </div>

        {/* Footer */}
        <div className="flex items-center flex-wrap" style={{ padding: "10px 14px", borderTop: "1px solid var(--si-border)", gap: 8 }}>
          <TemplatePicker onInsert={(text) => editor.current?.insertText(text)} />
          <AttachmentPicker attachments={attachments} onChange={setAttachments} />
          <div className="flex-1" />
          <Button variant="ghost" size="md" onClick={onClose}>Discard</Button>
          <Button variant="primary" size="md" icon={Send} onClick={send} disabled={!canSend}>
            {compose.isPending ? "Sending…" : "Send"}
          </Button>
        </div>
      </div>
    </div>
  );
}

function ComposeField({ label, children, noBorder }: {
  label: string; children: React.ReactNode; noBorder?: boolean;
}) {
  return (
    <div className="flex items-center gap-2.5" style={{ padding: "9px 0", borderBottom: noBorder ? undefined : "1px solid var(--si-border)" }}>
      <span style={{ width: 52, fontSize: 12, fontWeight: 600, color: "var(--si-text-3)" }}>{label}</span>
      {children}
    </div>
  );
}
