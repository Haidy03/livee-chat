import { format } from "date-fns";
import { ChannelIcon } from "../shared/ChannelIcon";
import { useCustomerStore } from "../stores";
import { ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";

const EMPTY_JOURNEY: ReturnType<typeof useCustomerStore.getState>["journeyByCustomer"][string] = [];

export function CustomerJourneyStrip({ customerId, onOpenFull }: { customerId: string; onOpenFull(): void }) {
  const journey = useCustomerStore((s) => s.journeyByCustomer[customerId] ?? EMPTY_JOURNEY);
  if (!journey.length) return null;
  return (
    <div className="flex items-center gap-2 overflow-x-auto border-b border-app-border bg-muted/30 px-3 py-1.5">
      <span className="text-[10px] uppercase tracking-wider text-muted-foreground">Journey</span>
      {journey.map((j, i) => (
        <div key={j.id} className="flex items-center gap-1.5">
          <div className="flex items-center gap-1 rounded-md border border-app-border bg-card px-1.5 py-0.5">
            <ChannelIcon channel={j.channel} size={11} withBackground={false} />
            <span className="text-[11px] font-medium">{j.agentName ?? "Agent"}</span>
            <span className="text-[10px] text-muted-foreground">{format(new Date(j.at), "MMM d")}</span>
          </div>
          {i < journey.length - 1 && <ChevronRight className="h-3 w-3 text-muted-foreground" />}
        </div>
      ))}
      <Button variant="link" size="sm" className="ms-auto h-6 px-1 text-xs" onClick={onOpenFull}>
        Full journey →
      </Button>
    </div>
  );
}
