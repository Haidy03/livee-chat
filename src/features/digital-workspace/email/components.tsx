import {
  forwardRef, useEffect, useImperativeHandle, useMemo, useRef, useState,
} from "react";
import { Bold, Italic, Underline, List, ListOrdered, Link as LinkIcon, RemoveFormatting, Paperclip, X, Zap, Search } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { getCurrentTenantId } from "@/lib/tenant";
import { listCannedResponses } from "@/features/canned-responses/api";
import { fileToAttachment, type AttachmentUpload } from "./api";

/* -------------------- HtmlEmailBody -------------------- */

/**
 * Renders an email's HTML body inside a sandboxed iframe so its styles can't leak into
 * the app and its scripts can never run (no allow-scripts). Height auto-fits content.
 */
export function HtmlEmailBody({ html }: { html: string }) {
  const ref = useRef<HTMLIFrameElement>(null);
  const [height, setHeight] = useState(120);

  const doc = `<!doctype html><html><head><base target="_blank">
<style>
  body { margin: 0; padding: 4px 2px; font-family: Arial, Helvetica, sans-serif;
         font-size: 13.5px; line-height: 1.6; color: #374151; word-break: break-word; }
  img { max-width: 100%; height: auto; }
  table { max-width: 100%; }
  a { color: #4f46e5; }
  blockquote { border-left: 3px solid #e5e7eb; margin: 8px 0; padding: 2px 12px; color: #6b7280; }
</style></head><body>${html}</body></html>`;

  const fit = () => {
    const body = ref.current?.contentDocument?.body;
    if (body) setHeight(Math.min(Math.max(body.scrollHeight + 12, 40), 2400));
  };

  return (
    <iframe
      ref={ref}
      title="email body"
      sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"
      srcDoc={doc}
      onLoad={fit}
      style={{ width: "100%", height, border: "none", display: "block", background: "transparent" }}
    />
  );
}

/* -------------------- DropMenu -------------------- */

export interface MenuItem {
  label: string;
  onClick: () => void;
  danger?: boolean;
}

/** Minimal dropdown anchored to its trigger; closes on outside click or item click. */
export function DropMenu({ trigger, items, align = "end", direction = "down" }: {
  trigger: (open: () => void) => React.ReactNode;
  items: MenuItem[];
  align?: "start" | "end";
  direction?: "up" | "down";
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  return (
    <div ref={ref} className="relative inline-block">
      {trigger(() => setOpen((v) => !v))}
      {open && (
        <div
          className="absolute z-50"
          style={{
            ...(direction === "up"
              ? { bottom: "calc(100% + 6px)" }
              : { top: "calc(100% + 6px)" }),
            [align === "end" ? "right" : "left"]: 0,
            minWidth: 190,
            maxHeight: 320,
            overflowY: "auto",
            background: "var(--si-panel)",
            border: "1px solid var(--si-border)",
            borderRadius: 10,
            boxShadow: "0 8px 24px rgba(15, 23, 42, .12)",
            padding: 5,
          }}
        >
          {items.map((item, i) => (
            <button
              key={i}
              type="button"
              onClick={() => { setOpen(false); item.onClick(); }}
              className="w-full text-left transition-colors hover:bg-[var(--si-panel-2)]"
              style={{
                display: "block", padding: "8px 11px", borderRadius: 7,
                fontSize: 12.5, fontWeight: 500,
                color: item.danger ? "#b91c1c" : "var(--si-text)",
              }}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

/* -------------------- RichComposer -------------------- */

export interface RichComposerHandle {
  getHtml: () => string;
  getText: () => string;
  setHtml: (html: string) => void;
  insertText: (text: string) => void;
  clear: () => void;
  focus: () => void;
}

/**
 * ContentEditable rich-text body with a working formatting toolbar. Output is read via
 * the imperative handle: getHtml() for the HTML part, getText() for the plain fallback.
 */
export const RichComposer = forwardRef<RichComposerHandle, {
  placeholder?: string;
  minHeight?: number;
  onTextChange?: (text: string) => void;
}>(function RichComposer({ placeholder = "Write your message…", minHeight = 96, onTextChange }, ref) {
  const editorRef = useRef<HTMLDivElement>(null);
  const [empty, setEmpty] = useState(true);

  const sync = () => {
    const text = editorRef.current?.innerText.trim() ?? "";
    setEmpty(text.length === 0);
    onTextChange?.(text);
  };

  useImperativeHandle(ref, () => ({
    getHtml: () => editorRef.current?.innerHTML ?? "",
    getText: () => editorRef.current?.innerText ?? "",
    setHtml: (html: string) => {
      if (editorRef.current) editorRef.current.innerHTML = html;
      sync();
    },
    insertText: (text: string) => {
      editorRef.current?.focus();
      document.execCommand("insertText", false, text);
      sync();
    },
    clear: () => {
      if (editorRef.current) editorRef.current.innerHTML = "";
      sync();
    },
    focus: () => editorRef.current?.focus(),
  }));

  const exec = (command: string, value?: string) => {
    editorRef.current?.focus();
    document.execCommand(command, false, value);
    sync();
  };

  const addLink = () => {
    const url = window.prompt("Link URL:");
    if (url) exec("createLink", /^https?:\/\//i.test(url) ? url : `https://${url}`);
  };

  return (
    <div>
      <div className="flex items-center" style={{ padding: "7px 14px", borderBottom: "1px solid var(--si-border)", gap: 2, color: "var(--si-text-2)" }}>
        <ToolBtn icon={Bold} label="Bold" onClick={() => exec("bold")} />
        <ToolBtn icon={Italic} label="Italic" onClick={() => exec("italic")} />
        <ToolBtn icon={Underline} label="Underline" onClick={() => exec("underline")} />
        <ToolDivider />
        <ToolBtn icon={List} label="Bulleted list" onClick={() => exec("insertUnorderedList")} />
        <ToolBtn icon={ListOrdered} label="Numbered list" onClick={() => exec("insertOrderedList")} />
        <ToolBtn icon={LinkIcon} label="Link" onClick={addLink} />
        <ToolDivider />
        <ToolBtn icon={RemoveFormatting} label="Clear formatting" onClick={() => exec("removeFormat")} />
      </div>
      <div className="relative">
        {empty && (
          <span
            aria-hidden
            style={{
              position: "absolute", top: 14, left: 16, fontSize: 13.5,
              color: "var(--si-text-3)", pointerEvents: "none",
            }}
          >
            {placeholder}
          </span>
        )}
        <div
          ref={editorRef}
          contentEditable
          role="textbox"
          aria-multiline="true"
          onInput={sync}
          className="w-full outline-none"
          style={{
            padding: "14px 16px",
            minHeight,
            maxHeight: 320,
            overflowY: "auto",
            fontSize: 13.5,
            lineHeight: 1.6,
            color: "var(--si-text)",
          }}
        />
      </div>
    </div>
  );
});

function ToolBtn({ icon: Icon, label, onClick }: { icon: any; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onMouseDown={(e) => e.preventDefault()} // keep editor selection
      onClick={onClick}
      className="inline-flex items-center justify-center transition-colors hover:bg-[var(--si-panel-2)] hover:text-[var(--si-text)]"
      style={{ width: 30, height: 30, borderRadius: 7 }}
    >
      <Icon size={15} />
    </button>
  );
}

function ToolDivider() {
  return <span style={{ width: 1, height: 18, background: "var(--si-border)", margin: "0 4px" }} />;
}

/* -------------------- TemplatePicker -------------------- */

/**
 * Searchable canned-response picker: title + content preview per row.
 * Renders nothing when no templates exist (manage them in System Settings).
 */
export function TemplatePicker({ onInsert, direction = "up" }: {
  onInsert: (text: string) => void;
  direction?: "up" | "down";
}) {
  const { data: templates = [] } = useQuery({
    queryKey: ["canned-responses", "email-composer"],
    queryFn: () => listCannedResponses(getCurrentTenantId()),
    staleTime: 60 * 1000,
    retry: false,
  });

  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  const visible = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return templates;
    return templates.filter((t) =>
      t.title.toLowerCase().includes(q) || t.messages.join(" ").toLowerCase().includes(q));
  }, [templates, query]);

  if (templates.length === 0) return null;

  return (
    <div ref={ref} className="relative inline-block">
      <button
        type="button"
        onClick={() => { setOpen((v) => !v); setQuery(""); }}
        className="inline-flex items-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)] hover:text-[var(--si-text)]"
        style={{ height: 32, padding: "0 9px", borderRadius: 8, fontSize: 12.5, fontWeight: 500, color: "var(--si-text-2)" }}
      >
        <Zap size={15} />
        Templates
      </button>
      {open && (
        <div
          className="absolute z-50 flex flex-col"
          style={{
            ...(direction === "up" ? { bottom: "calc(100% + 6px)" } : { top: "calc(100% + 6px)" }),
            left: 0,
            width: 340,
            maxHeight: 380,
            background: "var(--si-panel)",
            border: "1px solid var(--si-border)",
            borderRadius: 12,
            boxShadow: "0 12px 32px rgba(15, 23, 42, .16)",
            overflow: "hidden",
          }}
        >
          <div className="relative" style={{ padding: 8, borderBottom: "1px solid var(--si-border)" }}>
            <Search size={14} className="absolute" style={{ left: 18, top: "50%", transform: "translateY(-50%)", color: "var(--si-text-3)" }} />
            <input
              autoFocus
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search templates…"
              className="w-full outline-none"
              style={{
                height: 32, border: "1px solid var(--si-border-2)", borderRadius: 8,
                padding: "0 10px 0 30px", fontSize: 12.5, background: "var(--si-panel)", color: "var(--si-text)",
              }}
            />
          </div>
          <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: 5 }}>
            {visible.length === 0 && (
              <div style={{ padding: "16px 12px", fontSize: 12.5, color: "var(--si-text-3)", textAlign: "center" }}>
                No matching templates.
              </div>
            )}
            {visible.map((tpl) => (
              <button
                key={tpl._id}
                type="button"
                onClick={() => { setOpen(false); onInsert(tpl.messages.join("\n")); }}
                className="w-full text-left transition-colors hover:bg-[var(--si-panel-2)]"
                style={{ display: "block", padding: "8px 11px", borderRadius: 8 }}
              >
                <div style={{ fontSize: 12.5, fontWeight: 600, color: "var(--si-text)" }}>{tpl.title}</div>
                <div className="truncate" style={{ fontSize: 12, color: "var(--si-text-3)", marginTop: 2 }}>
                  {tpl.messages.join(" · ")}
                </div>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

/* -------------------- AttachmentPicker -------------------- */

const MAX_TOTAL_BYTES = 20 * 1024 * 1024;

/** Paperclip button + chips of picked files; owns the hidden file input. */
export function AttachmentPicker({ attachments, onChange }: {
  attachments: AttachmentUpload[];
  onChange: (next: AttachmentUpload[]) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);

  const pick = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    const current = [...attachments];
    let total = current.reduce((sum, a) => sum + a.base64Content.length * 0.75, 0);

    for (const file of Array.from(files)) {
      total += file.size;
      if (total > MAX_TOTAL_BYTES) {
        toast.error("Attachments exceed the 20 MB total limit.");
        break;
      }
      try {
        current.push(await fileToAttachment(file));
      } catch (err) {
        toast.error(err instanceof Error ? err.message : String(err));
      }
    }
    onChange(current);
    if (inputRef.current) inputRef.current.value = "";
  };

  return (
    <>
      <input ref={inputRef} type="file" multiple hidden onChange={(e) => pick(e.target.files)} />
      <button
        type="button"
        aria-label="Attach files"
        title="Attach files"
        onClick={() => inputRef.current?.click()}
        className="inline-flex items-center gap-1.5 transition-colors hover:bg-[var(--si-panel-2)] hover:text-[var(--si-text)]"
        style={{ height: 32, padding: "0 9px", borderRadius: 8, fontSize: 12.5, fontWeight: 500, color: "var(--si-text-2)" }}
      >
        <Paperclip size={15} />
        {attachments.length > 0 && <span>{attachments.length}</span>}
      </button>
      {attachments.length > 0 && (
        <div className="flex flex-wrap gap-1.5" style={{ padding: "0 4px" }}>
          {attachments.map((a, i) => (
            <span key={i} className="inline-flex items-center" style={{
              height: 24, padding: "0 4px 0 8px", borderRadius: 7, gap: 4,
              background: "#eef2f7", fontSize: 11.5, fontWeight: 500, color: "#334155",
              maxWidth: 180,
            }}>
              <span className="truncate">{a.fileName}</span>
              <button
                type="button"
                aria-label={`Remove ${a.fileName}`}
                onClick={() => onChange(attachments.filter((_, j) => j !== i))}
                className="inline-flex items-center justify-center hover:bg-[#dbe2eb]"
                style={{ width: 16, height: 16, borderRadius: 4, color: "#64748b" }}
              >
                <X size={11} />
              </button>
            </span>
          ))}
        </div>
      )}
    </>
  );
}

/* -------------------- snooze presets -------------------- */

export function snoozePresets(): { label: string; until: string }[] {
  const now = new Date();

  const inHours = (h: number) => new Date(now.getTime() + h * 3600_000);
  const tomorrow9 = new Date(now); tomorrow9.setDate(now.getDate() + 1); tomorrow9.setHours(9, 0, 0, 0);
  const nextWeek9 = new Date(now); nextWeek9.setDate(now.getDate() + 7); nextWeek9.setHours(9, 0, 0, 0);

  return [
    { label: "Later today (+4h)", until: inHours(4).toISOString() },
    { label: "Tomorrow 09:00", until: tomorrow9.toISOString() },
    { label: "Next week 09:00", until: nextWeek9.toISOString() },
  ];
}
