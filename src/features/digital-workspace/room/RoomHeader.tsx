import { useEffect, useState } from "react";
import { differenceInSeconds } from "date-fns";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  ArrowRightLeft,
  Bell,
  BellOff,
  CheckCircle2,
  ChevronsRight,
  CircleSlash,
  Flag,
  MailQuestion,
  MoreHorizontal,
  Mail,
  Phone,
  ShieldAlert,
  ShieldCheck,
  Star,
  Tag,
  UserPlus,
} from "lucide-react";
import { cn } from "@/lib/utils";
import type { Room } from "../models";
import { useAgentStore, useRoomStore, useCustomerStore, useQueueStore } from "../stores";
import { ChannelIcon } from "../shared/ChannelIcon";
import { SLAIndicator } from "../shared/SLAIndicator";
import { canAcceptRoom, canResolveRoom, canStartVoiceCall, canTransferRoom } from "../services/permissions";
import { acceptRoomAndOpen, declineRoom } from "../services/messageService";
import { voiceActionService } from "../services/VoiceActionService";
import { useToast } from "@/hooks/use-toast";

interface Props {
  room: Room;
  onResolve(): void;
  onTransfer(): void;
  onAssign(): void;
  onCollapse?: () => void;
}

export function RoomHeader({ room: c, onResolve, onTransfer, onAssign, onCollapse }: Props) {
  const customer = useCustomerStore((s) => s.byId[c.customerId]);
  const agent = useAgentStore((s) => (c.assignedAgentId ? s.byId[c.assignedAgentId] : undefined));
  const queue = useQueueStore((s) => s.byId[c.queueId]);
  const setStatus = useRoomStore((s) => s.setStatus);
  const closeTab = useRoomStore((s) => s.closeTab);
  const [, force] = useState(0);
  const { toast } = useToast();

  useEffect(() => {
    const id = setInterval(() => force((x) => x + 1), 1000);
    return () => clearInterval(id);
  }, []);

  if (!customer) return null;
  const initials = customer.name.split(" ").map((w) => w[0]).slice(0, 2).join("").toUpperCase();
  const processingFor = differenceInSeconds(new Date(), new Date(c.createdAt));
  const fmt = (s: number) => `${Math.floor(s / 60)}m ${(s % 60).toString().padStart(2, "0")}s`;

  return (
    <header className="flex flex-col gap-2.5 border-b border-app-border bg-card px-4 py-3">
      {/* Row 1: Avatar + primary actions */}
      <div className="flex items-center gap-3">
        <Avatar className="h-10 w-10">
          <AvatarFallback>{initials}</AvatarFallback>
        </Avatar>

        <div className="ms-auto flex items-center gap-2">
          {canAcceptRoom(c) ? (
            <>
              <Button
                size="sm"
                onClick={async () => {
                  try {
                    await acceptRoomAndOpen(c);
                    toast({ title: "Room accepted" });
                  } catch {
                    toast({ title: "Could not accept", variant: "destructive" });
                  }
                }}
              >
                Accept
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={async () => {
                  try {
                    await declineRoom(c.id);
                    closeTab(c.id);
                    useRoomStore.getState().removeRoom(c.id);
                    toast({ title: "Offer rejected" });
                  } catch {
                    toast({ title: "Could not decline", variant: "destructive" });
                  }
                }}
              >
                Reject
              </Button>
            </>
          ) : (
            <>
              {canResolveRoom(c) && (
                <Button
                  size="sm"
                  onClick={onResolve}
                  className="bg-emerald-600 text-white hover:bg-emerald-700"
                >
                  <CheckCircle2 className="me-1 h-3.5 w-3.5" /> Resolve
                </Button>
              )}
              {canTransferRoom(c) && (
                <Button size="sm" variant="outline" onClick={onTransfer}>
                  <ArrowRightLeft className="me-1 h-3.5 w-3.5" /> Transfer
                </Button>
              )}
            </>
          )}

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button size="sm" variant="outline" aria-label="More actions">
                <MoreHorizontal className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Assignment</DropdownMenuLabel>
              <DropdownMenuItem onClick={onAssign}><UserPlus className="me-2 h-3 w-3" /> Assign / Reassign</DropdownMenuItem>
              <DropdownMenuItem onClick={onTransfer}><ArrowRightLeft className="me-2 h-3 w-3" /> Transfer to queue</DropdownMenuItem>
              <DropdownMenuItem onClick={() => { setStatus(c.id, "escalated"); toast({ title: "Escalated to supervisor" }); }}>
                <Flag className="me-2 h-3 w-3" /> Escalate
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>State</DropdownMenuLabel>
              <DropdownMenuItem onClick={() => setStatus(c.id, "snoozed")}><BellOff className="me-2 h-3 w-3" /> Snooze</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setStatus(c.id, "pending")}><MailQuestion className="me-2 h-3 w-3" /> Set Pending</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setStatus(c.id, "spam")}><CircleSlash className="me-2 h-3 w-3" /> Mark spam</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Tags</DropdownMenuLabel>
              <DropdownMenuItem><Tag className="me-2 h-3 w-3" /> Add tag…</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                disabled={!canStartVoiceCall(c, !!customer.phone)}
                onClick={() =>
                  customer.phone &&
                  voiceActionService.startOutbound({
                    customerId: customer.id,
                    phoneNumber: customer.phone,
                    roomId: c.id,
                  }).then(() => toast({ title: "Voice call started (stub)" }))
                }
              >
                <Phone className="me-2 h-3 w-3" /> Start voice call
              </DropdownMenuItem>
              <DropdownMenuItem><Bell className="me-2 h-3 w-3" /> Invite supervisor</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          {onCollapse && (
            <Button size="icon" variant="ghost" className="h-7 w-7" onClick={onCollapse} aria-label="Collapse context">
              <ChevronsRight className="h-3.5 w-3.5" />
            </Button>
          )}
        </div>
      </div>

      {/* Row 1.5: Customer channel quick actions */}
      {(() => {
        const socialChannels = Array.from(
          new Set(
            customer.identities
              .map((i) => i.channel)
              .filter((ch) =>
                ["whatsapp", "messenger", "instagram_dm", "facebook_comment", "twitter_dm", "telegram"].includes(ch),
              ),
          ),
        );
        const hasAny = !!customer.phone || !!customer.email || socialChannels.length > 0;
        if (!hasAny) return null;
        return (
          <div className="flex flex-wrap items-center gap-2">
            {customer.phone && (
              <Button
                size="sm"
                variant="outline"
                className="h-7 rounded-full px-3"
                disabled={!canStartVoiceCall(c, true)}
                onClick={() =>
                  voiceActionService
                    .startOutbound({
                      customerId: customer.id,
                      phoneNumber: customer.phone!,
                      roomId: c.id,
                    })
                    .then(() => toast({ title: "Voice call started (stub)" }))
                }
              >
                <Phone className="me-1.5 h-3.5 w-3.5" /> Call
              </Button>
            )}
            {customer.email && (
              <Button
                size="sm"
                variant="outline"
                className="h-7 rounded-full px-3"
                onClick={() => toast({ title: "Compose email…", description: customer.email })}
              >
                <Mail className="me-1.5 h-3.5 w-3.5" /> Email
              </Button>
            )}
            {socialChannels.map((ch) => (
              <Button
                key={ch}
                size="sm"
                variant="outline"
                className="h-7 rounded-full px-2.5"
                onClick={() => toast({ title: `Open ${ch.replace(/_/g, " ")}` })}
              >
                <ChannelIcon channel={ch} size={14} withBackground={false} showLabel />
              </Button>
            ))}
          </div>
        );
      })()}


      {/* Row 2: Identity */}
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-1.5">
          <h1 className="truncate text-base font-semibold" dir="auto">{customer.name}</h1>
          {customer.vip && <Star className="h-4 w-4 fill-amber-400 text-amber-500" aria-label="VIP" />}
          {customer.authenticated ? (
            <ShieldCheck className="h-4 w-4 text-emerald-600" aria-label="Verified" />
          ) : (
            <ShieldAlert className="h-4 w-4 text-muted-foreground" aria-label="Unverified" />
          )}
          <Badge variant="outline" className="ms-1 gap-1 rounded-full border-emerald-500/30 bg-emerald-500/10 px-2 py-0.5 text-emerald-700 dark:text-emerald-400">
            <ChannelIcon channel={c.channel} size={12} withBackground={false} showLabel />
          </Badge>
        </div>
        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11px] text-muted-foreground">
          <span>ID: <span className="text-foreground/80">{c.id}</span></span>
          {c.caseId && <span>Case: <span className="text-foreground/80">{c.caseId}</span></span>}
          {queue && <span>Queue: <span className="text-foreground/80">{queue.name}</span></span>}
          {agent && <span>Agent: <span className="text-foreground/80">{agent.name}</span></span>}
          <span>Lang: <span className="text-foreground/80">{c.language.toUpperCase()}</span></span>
        </div>
      </div>

      {/* Row 3: Status / SLA / timer */}
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="outline" className="capitalize">{c.status.replace(/_/g, " ")}</Badge>
        <Badge variant="outline" className="capitalize">{c.priority}</Badge>
        <SLAIndicator sla={c.sla} />
        <span className="rounded-md bg-muted px-1.5 py-0.5 text-[10px] tabular-nums">⏱ {fmt(processingFor)}</span>
      </div>
    </header>
  );
}
