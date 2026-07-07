import { useEffect, useRef, useState } from "react";
import { EmailInboxColumn } from "./EmailInboxColumn";
import { EmailThreadColumn } from "./EmailThreadColumn";
import { EmailContextColumn } from "./EmailContextColumn";
import { ComposeDialog } from "./ComposeDialog";
import { useArchiveThread, useEmailThreads, useMarkThreadRead, useStarThread } from "./api";

interface ComposePrefill {
  to?: string;
  toName?: string;
}

export function EmailInbox() {
  const { data: threads = [], isLoading, refetch } = useEmailThreads();
  const markRead = useMarkThreadRead();
  const archive = useArchiveThread();
  const star = useStarThread();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [composing, setComposing] = useState<ComposePrefill | null>(null);
  const visibleIds = useRef<string[]>([]);

  // Auto-select the first thread once data arrives (or when the selected one disappears).
  useEffect(() => {
    if (threads.length === 0) {
      setSelectedId(null);
      return;
    }
    if (!selectedId || !threads.some((t) => t.id === selectedId)) {
      setSelectedId(threads[0].id);
    }
  }, [threads, selectedId]);

  const select = (id: string) => {
    setSelectedId(id);
    const thread = threads.find((t) => t.id === id);
    if (thread && thread.unreadCount > 0) markRead.mutate(id);
  };

  // Keyboard shortcuts: j/k navigate, e archive, s star. Ignored while typing.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      const target = e.target as HTMLElement | null;
      if (target && (target.tagName === "INPUT" || target.tagName === "TEXTAREA" || target.isContentEditable)) return;

      const ids = visibleIds.current;
      const idx = selectedId ? ids.indexOf(selectedId) : -1;

      if (e.key === "j" && ids.length > 0) {
        select(ids[Math.min(idx + 1, ids.length - 1)]);
      } else if (e.key === "k" && ids.length > 0) {
        select(ids[Math.max(idx - 1, 0)]);
      } else if (e.key === "e" && selectedId) {
        archive.mutate(selectedId);
      } else if (e.key === "s" && selectedId) {
        const t = threads.find((x) => x.id === selectedId);
        if (t) star.mutate({ threadId: t.id, starred: !t.starred });
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId, threads]);

  const selected = threads.find((t) => t.id === selectedId) ?? null;

  return (
    <div
      className="email-inbox flex h-full min-h-0 w-full overflow-hidden max-[900px]:flex-col"
      style={{ background: "var(--si-bg)" }}
    >
      <EmailInboxColumn
        threads={threads}
        loading={isLoading}
        selectedId={selectedId}
        onSelect={select}
        onRefresh={() => refetch()}
        onCompose={() => setComposing({})}
        onVisibleChange={(ids) => { visibleIds.current = ids; }}
      />
      <EmailThreadColumn thread={selected} />
      <EmailContextColumn thread={selected} onCompose={(prefill) => setComposing(prefill ?? {})} />
      {composing && (
        <ComposeDialog
          initialTo={composing.to}
          initialToName={composing.toName}
          onClose={() => setComposing(null)}
          onSent={(threadId) => setSelectedId(threadId)}
        />
      )}
    </div>
  );
}
