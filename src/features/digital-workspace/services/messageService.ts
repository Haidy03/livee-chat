import type { Room, Message, MessageAttachment } from "../models";
import { useMessageStore, useRoomStore, useInboxStore, useSessionStore } from "../stores";
import { createRealtimeClient } from "@/infrastructure/realtime/RealtimeClientFactory";
import { normalizeChannel } from "@/infrastructure/realtime/RealtimeEventDispatcher";

const client = createRealtimeClient();

export async function sendMessageOptimistic(args: {
  roomId: string;
  tenantId: string;
  senderId: string;
  channel: Message["channel"];
  text: string;
  internal?: boolean;
  publicReply?: boolean;
}): Promise<void> {
  const correlationId = `cli-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const provisional: Message = {
    id: correlationId, // temporary
    clientCorrelationId: correlationId,
    roomId: args.roomId,
    tenantId: args.tenantId,
    senderId: args.senderId,
    senderType: "agent",
    channel: normalizeChannel(args.channel as unknown as string),
    type: args.internal ? "internal_note" : "text",
    text: args.text,
    internal: args.internal,
    publicReply: !args.internal,
    status: "sending",
    sentAt: new Date().toISOString(),
    sequenceNumber: 0,
  };
  useMessageStore.getState().upsertMessage(provisional);
  useRoomStore.getState().touchLastMessage(args.roomId, provisional);

  try {
    await client.invoke("SendMessage", provisional);
  } catch (e) {
    useMessageStore.getState().setMessageStatus(args.roomId, correlationId, "failed");
  }
}

export async function retrySendMessage(roomId: string, messageId: string) {
  const msg = useMessageStore.getState().byId[messageId];
  if (!msg) return;
  useMessageStore.getState().setMessageStatus(roomId, messageId, "sending");
  try {
    await client.invoke("RetryMessage", msg);
  } catch {
    useMessageStore.getState().setMessageStatus(roomId, messageId, "failed");
  }
}

export async function sendTyping(roomId: string, isTyping: boolean) {
  try {
    await client.invoke("SendTyping", { roomId, isTyping });
  } catch {
    /* typing indicators are best-effort */
  }
}

export async function acceptRoom(requestId: string) {
  return client.invoke<unknown>("AcceptRoom", { requestId });
}

export async function acceptRoomAndOpen(room: Room) {
  await acceptRoom(room.id);

  // The backend also emits RoomStarted. If that event already migrated
  // the offer from request id -> room id, do not reinsert the old offer.
  const store = useRoomStore.getState();
  const current = store.byId[room.id];
  if (!current) return;

  const updated: Room = {
    ...current,
    status: "assigned",
    assignedAgentId: useSessionStore.getState().agentId || current.assignedAgentId,
    humanHandled: true,
    offerExpiresAt: undefined,
    updatedAt: new Date().toISOString(),
  };
  const nextStore = useRoomStore.getState();
  nextStore.upsertRoom(updated);
  nextStore.openTab(updated.id);
  useInboxStore.getState().setView("assigned_me");
}

export async function declineRoom(requestId: string) {
  await client.invoke("RejectRoom", { requestId });
}

export async function resolveRoom(roomId: string, typeOfClose = "resolved") {
  await client.invoke("ResolveRoom", { roomId, typeOfClose });
}

export async function transferRoom(roomId: string, targetId: string) {
  await client.invoke("TransferRoom", { roomId, targetId });
}

export async function sendAttachmentOptimistic(args: {
  roomId: string;
  tenantId: string;
  senderId: string;
  channel: Message["channel"];
  attachment: MessageAttachment;
  caption?: string;
}): Promise<void> {
  const correlationId = `cli-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const kindToType: Record<MessageAttachment["kind"], Message["type"]> = {
    image: "image",
    video: "video",
    audio: "audio",
    voice_note: "voice_note",
    document: "document",
  };
  const provisional: Message = {
    id: correlationId,
    clientCorrelationId: correlationId,
    roomId: args.roomId,
    tenantId: args.tenantId,
    senderId: args.senderId,
    senderType: "agent",
    channel: normalizeChannel(args.channel as unknown as string),
    type: kindToType[args.attachment.kind] ?? "document",
    text: args.caption,
    attachments: [args.attachment],
    publicReply: true,
    status: "sending",
    sentAt: new Date().toISOString(),
    sequenceNumber: 0,
  };
  useMessageStore.getState().upsertMessage(provisional);
  useRoomStore.getState().touchLastMessage(args.roomId, provisional);

  try {
    await client.invoke("SendAttachment", {
      roomId: args.roomId,
      attachment: args.attachment,
      caption: args.caption,
    });
  } catch (e) {
    useMessageStore.getState().setMessageStatus(args.roomId, correlationId, "failed");
    throw e;
  }
}

export async function setAgentPresence(status: string) {
  await client.invoke("SetAgentPresence", { status });
}
