import { useAgentStore } from "../stores";
import type { AgentPresence } from "../models";
import { setAgentPresence } from "../services/messageService";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/lib/utils";

const LABELS: Record<AgentPresence, { label: string; cls: string }> = {
  available: { label: "Available", cls: "bg-emerald-500" },
  busy: { label: "Busy", cls: "bg-amber-500" },
  away: { label: "Away", cls: "bg-orange-500" },
  break: { label: "Break", cls: "bg-blue-500" },
  offline: { label: "Offline", cls: "bg-muted-foreground" },
  after_contact_work: { label: "After-Contact Work", cls: "bg-purple-500" },
};

const WIRE: Record<AgentPresence, string> = {
  available: "Available",
  busy: "Busy",
  away: "Away",
  break: "Break",
  offline: "Offline",
  after_contact_work: "AfterContactWork",
};

export function AgentPresenceSelector() {
  const me = useAgentStore((s) => s.byId[s.meId]);
  const setPresence = useAgentStore((s) => s.setPresence);
  if (!me) return null;
  const handleChange = (v: AgentPresence) => {
    setPresence(v);
    void (async () => {
      try {
        await setAgentPresence(WIRE[v]);
      } catch (err) {
        console.warn("[AgentPresenceSelector] SetStatus failed", { status: WIRE[v], err });
      }
    })();
  };
  return (
    <Select value={me.presence} onValueChange={(v) => handleChange(v as AgentPresence)}>
      <SelectTrigger className="h-8 w-44 text-xs">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {(Object.keys(LABELS) as AgentPresence[]).map((k) => (
          <SelectItem key={k} value={k}>
            <span className="inline-flex items-center gap-2">
              <span className={cn("h-2 w-2 rounded-full", LABELS[k].cls)} aria-hidden />
              {LABELS[k].label}
            </span>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
