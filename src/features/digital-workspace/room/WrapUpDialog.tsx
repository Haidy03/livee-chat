import { useState } from "react";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import type { Room } from "../models";
import { useRoomStore } from "../stores";
import { useToast } from "@/hooks/use-toast";
import { resolveRoom } from "../services/messageService";

interface Props {
  open: boolean;
  onOpenChange(v: boolean): void;
  room: Room;
}

export function WrapUpDialog({ open, onOpenChange, room }: Props) {
  const setStatus = useRoomStore((s) => s.setStatus);
  const upsert = useRoomStore((s) => s.upsertRoom);
  const [form, setForm] = useState({
    contactReason: "shipping",
    category: "Order",
    subcategory: "Status",
    disposition: "resolved",
    code: "RES-200",
    outcome: "resolved",
    sentiment: room.sentiment,
    summary: "Customer asked about order status; provided ETA.",
    tags: "shipping, follow-up",
    followUp: false,
  });
  const { toast } = useToast();

  const submit = async (close = true) => {
    if (close) {
      try {
        await resolveRoom(room.id, form.disposition || "resolved");
      } catch {
        /* optimistic UI even if the hub call fails */
      }
      upsert({ ...room, status: "resolved", resolvedAt: new Date().toISOString(), updatedAt: new Date().toISOString() });
      toast({ title: "Room resolved" });
    } else {
      setStatus(room.id, "pending");
      toast({ title: "Saved as pending" });
    }
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Wrap up room</DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Contact reason">
            <Input value={form.contactReason} onChange={(e) => setForm((f) => ({ ...f, contactReason: e.target.value }))} />
          </Field>
          <Field label="Category">
            <Input value={form.category} onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))} />
          </Field>
          <Field label="Subcategory">
            <Input value={form.subcategory} onChange={(e) => setForm((f) => ({ ...f, subcategory: e.target.value }))} />
          </Field>
          <Field label="Disposition">
            <Select value={form.disposition} onValueChange={(v) => setForm((f) => ({ ...f, disposition: v }))}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="resolved">Resolved</SelectItem>
                <SelectItem value="escalated">Escalated</SelectItem>
                <SelectItem value="customer_no_response">Customer no response</SelectItem>
                <SelectItem value="duplicate">Duplicate</SelectItem>
                <SelectItem value="spam">Spam</SelectItem>
              </SelectContent>
            </Select>
          </Field>
          <Field label="Resolution code">
            <Input value={form.code} onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))} />
          </Field>
          <Field label="Sentiment">
            <Select value={form.sentiment} onValueChange={(v) => setForm((f) => ({ ...f, sentiment: v as any }))}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="positive">Positive</SelectItem>
                <SelectItem value="neutral">Neutral</SelectItem>
                <SelectItem value="negative">Negative</SelectItem>
              </SelectContent>
            </Select>
          </Field>
          <Field label="Tags" full>
            <Input value={form.tags} onChange={(e) => setForm((f) => ({ ...f, tags: e.target.value }))} />
          </Field>
          <Field label="Summary" full>
            <Textarea value={form.summary} onChange={(e) => setForm((f) => ({ ...f, summary: e.target.value }))} className="min-h-[80px]" />
          </Field>
          <label className="col-span-2 inline-flex items-center gap-2 text-xs">
            <Checkbox checked={form.followUp} onCheckedChange={(v) => setForm((f) => ({ ...f, followUp: !!v }))} />
            Follow-up required
          </label>
        </div>
        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button variant="secondary" onClick={() => void submit(false)}>Save as Pending</Button>
          <Button onClick={() => void submit(true)}>Resolve Room</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Field({ label, full, children }: { label: string; full?: boolean; children: React.ReactNode }) {
  return (
    <label className={`flex flex-col gap-1 text-xs ${full ? "col-span-2" : ""}`}>
      <span className="text-muted-foreground">{label}</span>
      {children}
    </label>
  );
}
