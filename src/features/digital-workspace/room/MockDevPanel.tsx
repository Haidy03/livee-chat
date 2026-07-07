import { useState } from "react";
import { ChevronUp, FlaskConical, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { isMockMode } from "@/infrastructure/realtime/RealtimeClientFactory";
import { getMockClient } from "@/infrastructure/realtime/MockRealtimeClient";
import { useRoomStore, useSessionStore } from "../stores";

/** Hidden in non-mock environments. Lets a developer fire scripted SignalR events. */
export function MockDevPanel() {
  const [open, setOpen] = useState(false);
  const tenantId = useSessionStore((s) => s.tenantId);
  const selectedId = useRoomStore((s) => s.selectedId);
  const conv = useRoomStore((s) => (selectedId ? s.byId[selectedId] : null));

  if (!isMockMode()) return null;
  const client = getMockClient();

  const emitMessage = () => {
    if (!conv) return;
    client.emit("MessageReceived", {
      event: "MessageReceived",
      tenantId,
      roomId: conv.id,
      serverSentAt: new Date().toISOString(),
      payload: {
        id: `srv-${Math.random().toString(36).slice(2, 8)}`,
        roomId: conv.id,
        tenantId,
        senderId: conv.customerId,
        senderType: "customer",
        channel: conv.channel,
        type: "text",
        text: conv.language === "ar" ? "هل وصلت رسالتي؟" : "Just checking in — any updates?",
        status: "delivered",
        sentAt: new Date().toISOString(),
        sequenceNumber: Date.now(),
      },
    });
  };

  const emitTyping = (on: boolean) => {
    if (!conv) return;
    client.emit("TypingChanged", {
      event: "TypingChanged",
      tenantId,
      roomId: conv.id,
      serverSentAt: new Date().toISOString(),
      payload: { roomId: conv.id, isTyping: on, who: "customer" },
    });
  };

  return (
    <div className={cn("fixed bottom-3 end-3 z-50 rounded-lg border border-app-border bg-card shadow-lg")}>
      <button
        onClick={() => setOpen(!open)}
        className="flex w-full items-center gap-2 px-3 py-1.5 text-xs font-semibold text-foreground"
      >
        <FlaskConical className="h-3.5 w-3.5 text-amber-600" />
        Mock Realtime
        {open ? <X className="ms-2 h-3 w-3" /> : <ChevronUp className="ms-2 h-3 w-3" />}
      </button>
      {open && (
        <div className="flex flex-col gap-1 border-t border-app-border p-2 text-xs">
          <Button size="sm" variant="outline" onClick={emitMessage} disabled={!conv}>Simulate incoming message</Button>
          <Button size="sm" variant="outline" onClick={() => emitTyping(true)} disabled={!conv}>Customer typing…</Button>
          <Button size="sm" variant="outline" onClick={() => emitTyping(false)} disabled={!conv}>Stop typing</Button>
          <Button size="sm" variant="outline" onClick={() => client.simulateDisconnect()}>Disconnect</Button>
          <Button size="sm" variant="outline" onClick={() => client.reconnect()}>Reconnect</Button>
        </div>
      )}
    </div>
  );
}
