import { useMemo, useState } from "react";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useRoomStore, useSessionStore } from "../stores";
import type { Room } from "../models";
import { useToast } from "@/hooks/use-toast";
import { transferRoom } from "../services/messageService";
import { useAgents, agentLabel } from "@/hooks/useAgents";
import { useQueues } from "@/features/queues/api";

interface Props {
  open: boolean;
  onOpenChange(v: boolean): void;
  room: Room;
  mode: "assign" | "transfer";
}

type Row = { id: string; name: string; subtitle: string };

export function AssignmentDialog({ open, onOpenChange, room, mode }: Props) {
  const upsert = useRoomStore((s) => s.upsertRoom);
  const currentAgentId = useSessionStore((s) => s.agentId);
  const agents = useAgents();
  const { data: queueRows = [], isLoading: queuesLoading } = useQueues();

  const [target, setTarget] = useState<string>("");
  const [kind, setKind] = useState<"agent" | "queue">(mode === "assign" ? "agent" : "queue");
  const [search, setSearch] = useState("");
  const [note, setNote] = useState("");
  const { toast } = useToast();

  const agentRows: Row[] = useMemo(
    () =>
      agents
        .filter((a) => a.user_id !== currentAgentId)
        .map((a) => ({
          id: a.user_id,
          name: agentLabel(a),
          subtitle: [a.role, a.email].filter(Boolean).join(" · "),
        })),
    [agents, currentAgentId],
  );

  const queueList: Row[] = useMemo(
    () =>
      queueRows
        .filter((q) => q.status === "active")
        .map((q) => ({ id: q.id, name: q.name, subtitle: `${q.channel} · ${q.status}` })),
    [queueRows],
  );

  const source = kind === "queue" ? queueList : agentRows;
  const loading = kind === "queue" ? queuesLoading : agents.length === 0;
  const list = source.filter((r) => !search || r.name.toLowerCase().includes(search.toLowerCase()));

  const submit = async () => {
    if (!target) return;
    const picked = source.find((r) => r.id === target);
    try {
      await transferRoom(room.id, target);
    } catch (e: any) {
      toast({ title: "Transfer failed", description: e?.message ?? "Unknown error", variant: "destructive" });
      return;
    }
    if (kind === "queue") {
      upsert({ ...room, queueId: target, assignedAgentId: undefined, status: "new", updatedAt: new Date().toISOString() });
      toast({ title: "Transferred to queue", description: picked?.name });
    } else {
      upsert({ ...room, assignedAgentId: target, status: "assigned", updatedAt: new Date().toISOString() });
      toast({ title: "Assigned", description: picked?.name });
    }
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{mode === "assign" ? "Assign room" : "Transfer room"}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div className="flex gap-2">
            <Select value={kind} onValueChange={(v) => { setKind(v as "agent" | "queue"); setTarget(""); }}>
              <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="agent">Agent</SelectItem>
                <SelectItem value="queue">Queue</SelectItem>
              </SelectContent>
            </Select>
            <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search…" />
          </div>
          <ul className="max-h-64 space-y-1 overflow-y-auto rounded-md border border-app-border p-1">
            {loading ? (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">Loading…</li>
            ) : list.length === 0 ? (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">
                No {kind === "queue" ? "queues" : "agents"} found
              </li>
            ) : (
              list.map((item) => (
                <li key={item.id}>
                  <button
                    onClick={() => setTarget(item.id)}
                    className={`flex w-full items-center justify-between rounded px-2 py-1.5 text-xs hover:bg-muted ${target === item.id ? "bg-primary/10 text-primary font-semibold" : ""}`}
                  >
                    <span className="truncate">{item.name}</span>
                    <span className="ms-2 shrink-0 text-[10px] text-muted-foreground">{item.subtitle}</span>
                  </button>
                </li>
              ))
            )}
          </ul>
          <Textarea value={note} onChange={(e) => setNote(e.target.value)} placeholder="Optional transfer note…" className="min-h-[60px] text-xs" />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={submit} disabled={!target}>{mode === "assign" ? "Assign" : "Transfer"}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
