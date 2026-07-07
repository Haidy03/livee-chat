import { useEffect, useRef, useState } from "react";
import { X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "../social/ui/primitives";
import { RichComposer, type RichComposerHandle } from "./components";
import { useEmailSignature, useSaveEmailSignature } from "./api";

/** Edit the agent's personal signature, appended below new replies and composes. */
export function SignatureDialog({ onClose }: { onClose: () => void }) {
  const { data: signature, isLoading } = useEmailSignature();
  const save = useSaveEmailSignature();
  const editor = useRef<RichComposerHandle>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    if (loaded || isLoading) return;
    editor.current?.setHtml(signature?.html ?? "");
    setLoaded(true);
  }, [signature, isLoading, loaded]);

  const submit = () => {
    save.mutate(editor.current?.getHtml() ?? "", {
      onSuccess: () => { toast.success("Signature saved"); onClose(); },
      onError: (err) => toast.error(err instanceof Error ? err.message : String(err)),
    });
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
          width: "min(560px, calc(100vw - 32px))",
          background: "var(--si-panel)",
          borderRadius: 14,
          border: "1px solid var(--si-border)",
          boxShadow: "0 24px 64px rgba(15, 23, 42, .25)",
          overflow: "hidden",
        }}
      >
        <div className="flex items-center" style={{ padding: "12px 16px", borderBottom: "1px solid var(--si-border)" }}>
          <h3 style={{ fontSize: 14.5, fontWeight: 700, color: "var(--si-text)" }}>Email signature</h3>
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

        <p style={{ padding: "10px 16px 0", fontSize: 12.5, color: "var(--si-text-2)" }}>
          Added automatically below your replies and new emails. Leave empty for no signature.
        </p>

        <RichComposer ref={editor} placeholder="e.g. Best regards, Tamer — Contact Center" minHeight={120} />

        <div className="flex items-center" style={{ padding: "10px 14px", borderTop: "1px solid var(--si-border)", gap: 8 }}>
          <div className="flex-1" />
          <Button variant="ghost" size="md" onClick={onClose}>Cancel</Button>
          <Button variant="primary" size="md" onClick={submit} disabled={save.isPending}>
            {save.isPending ? "Saving…" : "Save signature"}
          </Button>
        </div>
      </div>
    </div>
  );
}
