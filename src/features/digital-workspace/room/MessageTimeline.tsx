import { format, isSameDay } from "date-fns";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  AlertTriangle,
  Bot,
  Check,
  CheckCheck,
  Clock,
  Download,
  Info,
  Loader2,
  RotateCw,
  ShieldAlert,
} from "lucide-react";
import type { Room, Message } from "../models";
import { useMessageStore } from "../stores";
import { retrySendMessage } from "../services/messageService";
import { ChannelIcon } from "../shared/ChannelIcon";
import { sanitizeHtml } from "../services/sanitize";
import { EmptyState } from "../shared/States";

const EMPTY_MESSAGE_IDS: string[] = [];

interface Props {
  room: Room;
}

const StatusGlyph = ({ status }: { status: Message["status"] }) => {
  if (status === "sending") return <Loader2 className="h-3 w-3 animate-spin" aria-label="Sending" />;
  if (status === "failed") return <AlertTriangle className="h-3 w-3 text-destructive" aria-label="Failed" />;
  if (status === "read") return <CheckCheck className="h-3 w-3 text-primary" aria-label="Read" />;
  if (status === "delivered") return <CheckCheck className="h-3 w-3" aria-label="Delivered" />;
  return <Check className="h-3 w-3" aria-label="Sent" />;
};

export function MessageTimeline({ room }: Props) {
  const messageIds = useMessageStore((s) => s.byRoom[room.id] ?? EMPTY_MESSAGE_IDS);
  const byId = useMessageStore((s) => s.byId);
  const messages = useMemo(
    () => messageIds.map((id) => byId[id]).filter(Boolean) as Message[],
    [messageIds, byId],
  );

  const scrollRef = useRef<HTMLDivElement>(null);
  const [showJumpToLatest, setShowJumpToLatest] = useState(false);
  const isNearBottomRef = useRef(true);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    const onScroll = () => {
      const dist = el.scrollHeight - el.scrollTop - el.clientHeight;
      isNearBottomRef.current = dist < 80;
      setShowJumpToLatest(!isNearBottomRef.current);
    };
    el.addEventListener("scroll", onScroll);
    return () => el.removeEventListener("scroll", onScroll);
  }, []);

  useEffect(() => {
    const el = scrollRef.current;
    if (el && isNearBottomRef.current) el.scrollTop = el.scrollHeight;
  }, [messages.length]);

  if (messages.length === 0) {
    return <EmptyState title="No messages yet" description="Messages will appear here as soon as they arrive." />;
  }

  let lastDate = "";
  return (
    <div className="relative flex h-full min-h-0 flex-col">
      <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-3" role="log" aria-live="polite">
        <div className="mx-auto max-w-3xl space-y-2">
          {messages.map((m, i) => {
            const d = new Date(m.sentAt);
            const showDate = !lastDate || !isSameDay(new Date(lastDate), d);
            const node = (
              <MessageNode
                key={m.id}
                message={m}
                room={room}
                groupedWithPrev={
                  !showDate &&
                  i > 0 &&
                  messages[i - 1].senderType === m.senderType &&
                  messages[i - 1].senderId === m.senderId &&
                  m.type === "text" &&
                  messages[i - 1].type === "text"
                }
              />
            );
            const block = showDate ? (
              <div key={m.id} className="space-y-2">
                <DateSeparator date={d} />
                {node}
              </div>
            ) : (
              node
            );
            lastDate = m.sentAt;
            return block;
          })}
          {room.customerTyping && (
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span className="inline-flex gap-1">
                <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/60" />
                <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/60 [animation-delay:120ms]" />
                <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/60 [animation-delay:240ms]" />
              </span>
              Customer is typing…
            </div>
          )}
        </div>
      </div>
      {showJumpToLatest && (
        <Button
          size="sm"
          variant="secondary"
          className="absolute bottom-4 end-6 shadow"
          onClick={() => {
            const el = scrollRef.current;
            if (el) el.scrollTop = el.scrollHeight;
          }}
        >
          Jump to latest
        </Button>
      )}
    </div>
  );
}

function DateSeparator({ date }: { date: Date }) {
  return (
    <div className="my-3 flex items-center gap-3">
      <div className="h-px flex-1 bg-app-border" />
      <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] uppercase tracking-wider text-muted-foreground">
        {format(date, "EEE, MMM d")}
      </span>
      <div className="h-px flex-1 bg-app-border" />
    </div>
  );
}

function MessageNode({
  message: m,
  room,
  groupedWithPrev,
}: {
  message: Message;
  room: Room;
  groupedWithPrev: boolean;
}) {
  if (m.senderType === "system") {
    return <SystemEvent message={m} />;
  }
  if (m.internal || m.type === "internal_note") {
    return <InternalNote message={m} />;
  }

  const isAgent = m.senderType === "agent" || m.senderType === "supervisor";
  const isBot = m.senderType === "bot";

  return (
    <div
      className={cn("flex w-full", isAgent ? "justify-end" : "justify-start", groupedWithPrev && "-mt-1")}
      dir="auto"
    >
      <div
        className={cn(
          "max-w-[78%] rounded-2xl px-3 py-2 text-sm shadow-sm",
          isAgent
            ? "rounded-ee-sm bg-primary text-primary-foreground"
            : isBot
              ? "rounded-es-sm border border-app-border bg-muted/60 text-foreground"
              : "rounded-es-sm border border-app-border bg-card text-foreground",
        )}
      >
        {!groupedWithPrev && (
          <div className="mb-1 flex items-center gap-1.5 text-[10px] opacity-80">
            {isBot && <Bot className="h-3 w-3" aria-label="Bot" />}
            <ChannelIcon channel={m.channel} size={10} withBackground={false} />
            <span>{format(new Date(m.sentAt), "HH:mm")}</span>
          </div>
        )}
        {m.html ? (
          <div className="whitespace-pre-wrap break-words" dangerouslySetInnerHTML={{ __html: sanitizeHtml(m.html) }} />
        ) : (
          <p className="whitespace-pre-wrap break-words">{m.text}</p>
        )}
        {m.attachments?.map((a) => (
          <div key={a.id} className="mt-2 rounded-md border border-current/20 bg-background/40 p-2 text-xs">
            <div className="flex items-center gap-2">
              <Download className="h-3 w-3" />
              <span className="truncate">{a.name ?? a.kind}</span>
              {a.sizeBytes && (
                <span className="ms-auto opacity-70">{Math.round(a.sizeBytes / 1024)} KB</span>
              )}
            </div>
            {a.kind === "image" && (
              <img src={a.url} alt={a.name ?? "image"} className="mt-2 max-h-48 rounded" />
            )}
          </div>
        ))}
        {isAgent && (
          <div className="mt-1 flex items-center justify-end gap-1 text-[10px] opacity-80">
            <StatusGlyph status={m.status} />
            {m.status === "failed" && (
              <button
                className="underline"
                onClick={() => retrySendMessage(room.id, m.id)}
                aria-label="Retry"
              >
                <RotateCw className="inline h-3 w-3" /> Retry
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function InternalNote({ message: m }: { message: Message }) {
  return (
    <div className="rounded-md border border-amber-300/60 bg-amber-50 px-3 py-2 text-xs dark:border-amber-700/60 dark:bg-amber-950/30">
      <div className="mb-1 flex items-center gap-1 font-semibold text-amber-800 dark:text-amber-300">
        <ShieldAlert className="h-3 w-3" />
        Internal note — not visible to the customer
      </div>
      <p className="whitespace-pre-wrap break-words text-amber-900 dark:text-amber-100" dir="auto">
        {m.text}
      </p>
      <p className="mt-1 text-[10px] text-amber-700/80 dark:text-amber-400/80">
        {format(new Date(m.sentAt), "MMM d, HH:mm")}
      </p>
    </div>
  );
}

function SystemEvent({ message: m }: { message: Message }) {
  const Icon = m.type === "sla_warning" ? AlertTriangle : Info;
  const cls =
    m.type === "sla_warning"
      ? "text-amber-700 dark:text-amber-400"
      : "text-muted-foreground";
  return (
    <div className={cn("flex items-center justify-center gap-1.5 text-[11px]", cls)}>
      <Icon className="h-3 w-3" />
      <span>{m.text}</span>
      <Clock className="h-3 w-3 opacity-60" />
      <span className="opacity-70">{format(new Date(m.sentAt), "HH:mm")}</span>
    </div>
  );
}
