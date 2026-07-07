import { useState } from "react";
import { formatDistanceToNowStrict } from "date-fns";
import { cn } from "@/lib/utils";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { ChannelIcon } from "../shared/ChannelIcon";
import { SLAIndicator } from "../shared/SLAIndicator";
import type { Room } from "../models";
import { useRoomStore, useCustomerStore } from "../stores";
import { acceptRoomAndOpen, declineRoom } from "../services/messageService";
import { useToast } from "@/hooks/use-toast";
import { AlertOctagon, Bot, MessagesSquare, X } from "lucide-react";
import { WrapUpDialog } from "../room/WrapUpDialog";
import { ConversationTimers } from "./ConversationTimers";

interface Props {
  room: Room;
  selected: boolean;
  onSelect: () => void;
  compact?: boolean;
  showCloseAction?: boolean;
}

const PRIORITY_DOT: Record<Room["priority"], string> = {
  urgent: "bg-destructive",
  high: "bg-amber-500",
  normal: "bg-muted-foreground/40",
  low: "bg-muted-foreground/20",
};

export function RoomListItem({ room: c, selected, onSelect, compact, showCloseAction }: Props) {
  const customer = useCustomerStore((s) => s.byId[c.customerId]);
  const { toast } = useToast();
  const [wrapOpen, setWrapOpen] = useState(false);
  const name = customer?.name ?? "Unknown";
  const initials = name
    .split(" ")
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
  const unread = c.unreadCount > 0;
  const isOffered = c.status === "offered";

  const onAccept = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await acceptRoomAndOpen(c);
      toast({ title: "Room accepted" });
    } catch {
      toast({ title: "Could not accept", variant: "destructive" });
    }
  };
  const onDecline = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await declineRoom(c.id);
      useRoomStore.getState().removeRoom(c.id);
      toast({ title: "Offer rejected" });
    } catch {
      toast({ title: "Could not decline", variant: "destructive" });
    }
  };

  const canClose = showCloseAction && !isOffered && c.status !== "resolved";

  return (
    <>
    <div
      onClick={onSelect}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onSelect();
        }
      }}
      dir="auto"
      className={cn(
        "relative flex w-full cursor-pointer gap-2.5 border-b border-app-border px-3 text-start transition-colors",
        compact ? "py-2" : "py-2.5",
        selected ? "bg-primary/8" : "hover:bg-muted/60",
        unread && "bg-primary/[0.04]",
        isOffered && "bg-amber-50 dark:bg-amber-950/20",
      )}
      aria-current={selected ? "true" : undefined}
      role="listitem"
      tabIndex={0}
    >
      {selected && <span className="absolute inset-y-0 start-0 w-0.5 bg-primary" aria-hidden />}
      <div className="flex shrink-0 gap-2.5">
        <span
          className={cn("mt-1 inline-block h-2 w-2 shrink-0 rounded-full", PRIORITY_DOT[c.priority])}
          aria-label={`Priority ${c.priority}`}
        />
        <Avatar className="h-9 w-9 shrink-0">
          <AvatarFallback className="text-[11px]">{initials}</AvatarFallback>
        </Avatar>
      </div>
      <div className="min-w-0 flex-1">
        <div>
          <div className="flex items-center gap-2">
            <span className={cn("truncate text-sm", unread ? "font-semibold" : "font-medium")}>{name}</span>
            {c.botHandled && <Bot className="h-3 w-3 text-muted-foreground" aria-label="Bot handled" />}
            <span className="ms-auto text-[10px] tabular-nums text-muted-foreground">
              {formatDistanceToNowStrict(new Date(c.lastMessageAt), { addSuffix: false })}
            </span>
          </div>
          <div className="mt-0.5 flex items-center gap-1.5">
            <ChannelIcon channel={c.channel} size={11} withBackground={false} />
            <p
              className={cn("min-w-0 flex-1 truncate text-xs", unread ? "text-foreground" : "text-muted-foreground")}
            >
              {c.customerTyping ? <em className="text-primary">typing…</em> : c.lastMessagePreview}
            </p>
          </div>
          <div className="mt-1 flex items-center gap-2">
            <SLAIndicator sla={c.sla} />
            {c.status === "escalated" && (
              <span className="inline-flex items-center gap-0.5 text-[10px] font-medium text-destructive">
                <AlertOctagon className="h-3 w-3" /> Escalated
              </span>
            )}
            {c.tags.slice(0, 2).map((t) => (
              <span key={t} className="rounded bg-muted px-1 py-0.5 text-[10px] text-muted-foreground">
                {t}
              </span>
            ))}
            {unread && (
              <span className="ms-auto inline-flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold text-primary-foreground tabular-nums">
                {c.unreadCount}
              </span>
            )}
            {canClose && (
              <Button
                size="icon"
                variant="ghost"
                className={cn("h-6 w-6", !unread && "ms-auto")}
                aria-label="Close conversation"
                onClick={(e) => {
                  e.stopPropagation();
                  setWrapOpen(true);
                }}
              >
                <X className="h-3.5 w-3.5" />
              </Button>
            )}
          </div>
          <ConversationTimers roomId={c.id} />
        </div>
        {isOffered && (
          <div className="mt-1.5 flex items-center gap-1.5">
            <MessagesSquare className="h-3 w-3 text-amber-700 dark:text-amber-400" />
            <span className="text-[10px] font-semibold text-amber-700 dark:text-amber-400">New offer</span>
            <div className="ms-auto flex items-center gap-1">
              <Button size="sm" variant="outline" className="h-6 px-2 text-[10px]" onClick={onDecline}>
                Reject
              </Button>
              <Button size="sm" className="h-6 px-2 text-[10px]" onClick={onAccept}>
                Accept
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
    {canClose && <WrapUpDialog open={wrapOpen} onOpenChange={setWrapOpen} room={c} />}
    </>
  );
}
