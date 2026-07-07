import { cn } from "@/lib/utils";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { ChannelIcon } from "../shared/ChannelIcon";
import { channelColor } from "../shared/channelTokens";
import type { Room } from "../models";
import { useCustomerStore } from "../stores";
import { AlertTriangle, AlertOctagon, CheckCircle2 } from "lucide-react";

interface Props {
  room: Room;
  selected: boolean;
  onSelect: () => void;
}

export function RoomRailItem({ room: c, selected, onSelect }: Props) {
  const customer = useCustomerStore((s) => s.byId[c.customerId]);
  const name = customer?.name ?? "Unknown";
  const initials = name
    .split(" ")
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  const slaColor =
    c.sla.state === "ok"
      ? "text-emerald-600 dark:text-emerald-400"
      : c.sla.state === "warning"
        ? "text-amber-600 dark:text-amber-400"
        : "text-destructive";
  const SlaIcon = c.sla.state === "ok" ? CheckCircle2 : c.sla.state === "warning" ? AlertTriangle : AlertOctagon;

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          onClick={onSelect}
          dir="auto"
          className={cn(
            "relative flex w-full flex-col items-center gap-1 border-b border-app-border py-2.5 transition-colors",
            selected ? "bg-primary/8" : "hover:bg-muted/70",
            c.unreadCount > 0 && "bg-primary/[0.04]",
          )}
          aria-current={selected ? "true" : undefined}
          aria-label={`${name} — ${c.channel}`}
        >
          {selected && (
            <span className="absolute inset-y-0 start-0 w-0.5 bg-primary" aria-hidden />
          )}

          <div className="relative">
            <Avatar className="h-9 w-9 shrink-0">
              <AvatarFallback className="text-[11px]">{initials}</AvatarFallback>
            </Avatar>
            {c.unreadCount > 0 && (
              <span className="absolute -end-1.5 -top-1 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold text-primary-foreground tabular-nums">
                {c.unreadCount > 9 ? "9+" : c.unreadCount}
              </span>
            )}
          </div>

          <div className="flex items-center gap-1">
            <ChannelIcon channel={c.channel} size={12} withBackground={false} />
            {c.status === "escalated" && (
              <AlertOctagon className="h-3 w-3 text-destructive" aria-label="Escalated" />
            )}
            <SlaIcon className={cn("h-3 w-3", slaColor)} aria-label={`SLA ${c.sla.state}`} />
          </div>
        </button>
      </TooltipTrigger>
      <TooltipContent side="right" align="center" className="max-w-[16rem]">
        <div className="space-y-1">
          <p className="font-semibold">{name}</p>
          <p className="text-muted-foreground flex items-center gap-1 text-xs">
            <ChannelIcon channel={c.channel} size={12} withBackground={false} />
            {c.channel.replace(/_/g, " ")}
          </p>
          <p className="text-xs text-muted-foreground truncate">{c.lastMessagePreview}</p>
        </div>
      </TooltipContent>
    </Tooltip>
  );
}
