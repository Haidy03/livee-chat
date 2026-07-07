import { useEffect, useRef, useState } from "react";
import { useCopilotMessagesContext } from "@copilotkit/react-core";

const MAX_MESSAGES = 200;
const MAX_BYTES = 1_000_000; // ~1 MB
const DEBOUNCE_MS = 300;

const clearedThreadIds = new Set<string>();

export function messagesStorageKey(threadId: string) {
  return `alk:copilot:messages:${threadId}`;
}

export function messagesTimestampKey(threadId: string) {
  return `alk:copilot:messages:${threadId}:ts`;
}

export function clearPersistedMessages(threadId: string) {
  clearedThreadIds.add(threadId);
  try {
    localStorage.removeItem(messagesStorageKey(threadId));
    localStorage.removeItem(messagesTimestampKey(threadId));
  } catch {
    /* ignore */
  }
}

function wasCleared(threadId: string) {
  return clearedThreadIds.has(threadId);
}

function markWritten(threadId: string) {
  clearedThreadIds.delete(threadId);
}

function readCached(threadId: string): any[] | null {
  try {
    const raw = localStorage.getItem(messagesStorageKey(threadId));
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function readCachedTimestamp(threadId: string): number | null {
  try {
    const raw = localStorage.getItem(messagesTimestampKey(threadId));
    if (!raw) return null;
    const ts = Number(raw);
    return Number.isFinite(ts) && ts > 0 ? ts : null;
  } catch {
    return null;
  }
}

function writeCached(threadId: string, messages: any[]) {
  try {
    let trimmed = messages.slice(-MAX_MESSAGES);
    let serialized = JSON.stringify(trimmed);
    while (serialized.length > MAX_BYTES && trimmed.length > 1) {
      trimmed = trimmed.slice(Math.ceil(trimmed.length / 4));
      serialized = JSON.stringify(trimmed);
    }
    localStorage.setItem(messagesStorageKey(threadId), serialized);
    localStorage.setItem(messagesTimestampKey(threadId), Date.now().toString());
    markWritten(threadId);
    return trimmed;
  } catch {
    return null;
  }
}

interface Props {
  threadId: string;
}

/**
 * Bridges the CopilotKit v1 message context to localStorage so rooms survive
 * a hard page reload (client-side cache only). Must render inside <CopilotKit>.
 */
export function MessagePersistenceBridge({ threadId }: Props) {
  const { messages, setMessages } = useCopilotMessagesContext();
  const hydratedRef = useRef<string | null>(null);
  const debounceRef = useRef<number | null>(null);

  // Hydrate from cache once per threadId.
  useEffect(() => {
    if (hydratedRef.current === threadId) return;
    hydratedRef.current = threadId;

    const cached = readCached(threadId);
    if (cached && cached.length > 0 && (!messages || messages.length === 0)) {
      try {
        setMessages(cached as never);
      } catch {
        /* ignore */
      }
    }
  }, [threadId, messages, setMessages]);

  // Persist message list changes (debounced).
  useEffect(() => {
    if (wasCleared(threadId)) return;
    if (!messages || messages.length === 0) return;
    if (debounceRef.current != null) window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      debounceRef.current = null;
      writeCached(threadId, messages as any[]);
    }, DEBOUNCE_MS);
    return () => {
      if (debounceRef.current != null) {
        window.clearTimeout(debounceRef.current);
        debounceRef.current = null;
      }
    };
  }, [messages, threadId]);

  return <PersistenceDebugOverlay threadId={threadId} cachedCount={(messages ?? []).length} />;
}

function isDebugEnabled() {
  try {
    if (import.meta.env?.DEV) return true;
    return localStorage.getItem("alk:copilot:debug") === "1";
  } catch {
    return false;
  }
}

function PersistenceDebugOverlay({
  threadId,
  cachedCount,
}: {
  threadId: string;
  cachedCount: number;
}) {
  const [lastSaved, setLastSaved] = useState<number | null>(() => readCachedTimestamp(threadId));

  useEffect(() => {
    setLastSaved(readCachedTimestamp(threadId));
    const id = window.setInterval(() => setLastSaved(readCachedTimestamp(threadId)), 1000);
    return () => window.clearInterval(id);
  }, [threadId]);

  if (!isDebugEnabled()) return null;
  const shortThread =
    threadId.length > 12 ? `${threadId.slice(0, 6)}…${threadId.slice(-4)}` : threadId;
  const found = cachedCount > 0;
  return (
    <div
      style={{
        position: "absolute",
        top: 6,
        right: 6,
        zIndex: 50,
        font: "11px/1.3 ui-monospace, SFMono-Regular, Menlo, monospace",
        background: "rgba(17,17,17,0.78)",
        color: "#fff",
        padding: "4px 8px",
        borderRadius: 6,
        pointerEvents: "none",
        userSelect: "none",
        maxWidth: 260,
      }}
      data-testid="copilot-persistence-debug"
    >
      <div>
        <span style={{ opacity: 0.7 }}>thread </span>
        <span>{shortThread}</span>
      </div>
      <div>
        <span style={{ opacity: 0.7 }}>cache </span>
        <span style={{ color: found ? "#4ade80" : "#f87171" }}>
          {found ? `hit (${cachedCount})` : "miss"}
        </span>
      </div>
      <div>
        <span style={{ opacity: 0.7 }}>saved </span>
        <span>{formatLastSaved(lastSaved)}</span>
      </div>
    </div>
  );
}

function formatLastSaved(ts: number | null): string {
  if (!ts) return "never";
  const now = Date.now();
  const diffSec = Math.round((now - ts) / 1000);
  if (diffSec < 5) return "just now";
  if (diffSec < 60) return `${diffSec}s ago`;
  if (diffSec < 3600) return `${Math.floor(diffSec / 60)}m ago`;
  if (diffSec < 86400) return `${Math.floor(diffSec / 3600)}h ago`;
  return new Date(ts).toLocaleDateString();
}
