import { Mail, Check, Clock, Archive, MessagesSquare, CalendarClock, AtSign, Inbox } from "lucide-react";
import { Avatar, Button, Pill } from "../social/ui/primitives";
import {
  avatarColorOf, formatSnoozeUntil, initialsOf, useEmailMessages,
  type EmailThread,
} from "./api";

export function EmailContextColumn({
  thread, onCompose,
}: {
  thread: EmailThread | null;
  onCompose: (prefill?: { to: string; toName?: string }) => void;
}) {
  const { data: messages = [] } = useEmailMessages(thread?.id ?? null);

  if (!thread) {
    return (
      <aside
        className="flex flex-col items-center justify-center min-h-0 gap-3 max-[1180px]:w-[300px] max-[900px]:hidden"
        style={{ width: 348, background: "var(--si-panel)", borderLeft: "1px solid var(--si-border)" }}
      >
        <Inbox size={22} style={{ color: "var(--si-text-3)" }} />
        <span style={{ fontSize: 12.5, color: "var(--si-text-3)" }}>No conversation selected</span>
      </aside>
    );
  }

  const inbound = messages.filter((m) => m.direction === "inbound");
  const outbound = messages.filter((m) => m.direction === "outbound");
  const firstAt = messages.length > 0 ? messages[0].sentAt : thread.lastMessageAt;
  const snoozed = thread.snoozedUntil && new Date(thread.snoozedUntil) > new Date();

  return (
    <aside
      className="flex flex-col min-h-0 max-[1180px]:w-[300px] max-[900px]:hidden"
      style={{ width: 348, background: "var(--si-panel)", borderLeft: "1px solid var(--si-border)" }}
    >
      {/* Customer header */}
      <div className="flex flex-col items-center text-center" style={{ padding: "22px 16px 16px", borderBottom: "1px solid var(--si-border)" }}>
        <Avatar
          initials={initialsOf(thread.counterpartName)}
          color={avatarColorOf(thread.counterpartEmail)}
        />
        <div style={{ fontSize: 15, fontWeight: 700, color: "var(--si-text)", marginTop: 10 }}>
          {thread.counterpartName}
        </div>
        <div className="flex items-center gap-1" style={{ fontSize: 12.5, color: "var(--si-text-2)", marginTop: 2 }}>
          <AtSign size={12} />
          {thread.counterpartEmail}
        </div>
        <div style={{ marginTop: 12 }}>
          {thread.status === "resolved" && <Pill variant="green" icon={Check}>Resolved</Pill>}
          {thread.status === "archived" && <Pill variant="default" icon={Archive}>Archived</Pill>}
          {thread.status === "open" && !snoozed && <Pill variant="green">Active</Pill>}
          {snoozed && <Pill variant="default" icon={Clock}>Snoozed</Pill>}
        </div>
        <Button
          variant="ghost"
          size="sm"
          icon={Mail}
          className="mt-3"
          onClick={() => onCompose({ to: thread.counterpartEmail, toName: thread.counterpartName })}
        >
          New email to {thread.counterpartName.split(" ")[0]}
        </Button>
      </div>

      {/* Conversation facts */}
      <div className="flex-1 min-h-0 overflow-y-auto" style={{ padding: "14px 16px" }}>
        <SectionTitle>Conversation</SectionTitle>
        <Row icon={MessagesSquare} label="Messages" value={`${thread.messageCount}`} />
        <Row icon={Inbox} label="From customer" value={`${inbound.length}`} />
        <Row icon={Mail} label="Your replies" value={`${outbound.length}`} />
        <Row icon={CalendarClock} label="First contact" value={formatFull(firstAt)} />
        <Row icon={Clock} label="Last activity" value={formatFull(thread.lastMessageAt)} />
        {snoozed && (
          <Row icon={Clock} label="Snoozed until" value={formatSnoozeUntil(thread.snoozedUntil!)} />
        )}

        <SectionTitle className="mt-5">Mailbox</SectionTitle>
        <Row icon={AtSign} label="Received at" value={thread.mailbox} />

        {(() => {
          const files = messages.flatMap((m) => m.attachmentNames);
          if (files.length === 0) return null;
          return (
            <>
              <SectionTitle className="mt-5">Attachments ({files.length})</SectionTitle>
              {files.map((name, i) => (
                <div key={i} className="truncate" style={{ fontSize: 12.5, color: "var(--si-text-2)", padding: "3px 0" }}>
                  {name}
                </div>
              ))}
            </>
          );
        })()}
      </div>
    </aside>
  );
}

function SectionTitle({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div
      className={`uppercase font-bold ${className ?? ""}`}
      style={{ fontSize: 11, letterSpacing: "0.04em", color: "var(--si-text-3)", margin: "0 0 8px" }}
    >
      {children}
    </div>
  );
}

function Row({ icon: Icon, label, value }: { icon: any; label: string; value: string }) {
  return (
    <div className="flex items-center gap-2.5" style={{ padding: "6px 0" }}>
      <Icon size={15} style={{ color: "var(--si-text-3)", flexShrink: 0 }} />
      <span style={{ fontSize: 12.5, color: "var(--si-text-2)" }}>{label}</span>
      <span className="ms-auto truncate" style={{ fontSize: 12.5, fontWeight: 600, color: "var(--si-text)", maxWidth: 180, textAlign: "end" }}>
        {value}
      </span>
    </div>
  );
}

function formatFull(iso: string): string {
  return new Date(iso).toLocaleString([], {
    month: "short", day: "numeric", hour: "2-digit", minute: "2-digit", hour12: false,
  });
}
