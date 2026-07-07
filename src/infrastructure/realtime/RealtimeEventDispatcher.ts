/**
 * Single chokepoint between the realtime transport and zustand stores.
 * Components never call store methods in response to a wire event — they
 * subscribe to stores, and the dispatcher does the writes.
 */
import type { IRealtimeClient, Unsubscribe } from "./IRealtimeClient";
import type { Channel, Room, Customer, Message } from "@/features/digital-workspace/models";

const KNOWN_CHANNELS: readonly Channel[] = [
  "web_chat",
  "mobile_app",
  "whatsapp",
  "messenger",
  "instagram_dm",
  "instagram_comment",
  "facebook_comment",
  "twitter_dm",
  "twitter_mention",
  "telegram",
];

const CHANNEL_ALIASES: Record<string, Channel> = {
  webwidget: "web_chat",
  web: "web_chat",
  widget: "web_chat",
  webchat: "web_chat",
  mobile: "mobile_app",
  app: "mobile_app",
  wa: "whatsapp",
  fb_messenger: "messenger",
  ig_dm: "instagram_dm",
  ig_comment: "instagram_comment",
  fb_comment: "facebook_comment",
  tw_dm: "twitter_dm",
  tw_mention: "twitter_mention",
};

const warnedChannels = new Set<string>();
export function normalizeChannel(raw?: string): Channel {
  const key = String(raw ?? "").trim().toLowerCase();
  if ((KNOWN_CHANNELS as readonly string[]).includes(key)) return key as Channel;
  const mapped = CHANNEL_ALIASES[key];
  if (mapped) return mapped;
  if (!warnedChannels.has(key)) {
    warnedChannels.add(key);
    console.warn(`[realtime] Unknown channel "${raw}", defaulting to web_chat`);
  }
  return "web_chat";
}
import {
  useRoomStore,
  useCustomerStore,
  useInboxStore,
  useMessageStore,
  useRealtimeStore,
  useSessionStore,
} from "@/features/digital-workspace/stores";


interface RequestOfferedPayload {
  requestId: string;
  channel: string;
  department?: { id?: string | null; name?: string | null } | null;
  lang?: string;
  clientInfo?: string;
  requestCount?: number;
}

function field<T = unknown>(raw: any, camel: string, pascal?: string): T | undefined {
  return raw?.[camel] ?? raw?.[pascal ?? `${camel.charAt(0).toUpperCase()}${camel.slice(1)}`];
}

function parseClientInfo(raw?: string): { name?: string; email?: string; userAgent?: string; url?: string } {
  if (!raw) return {};
  try {
    const obj = JSON.parse(raw);
    return {
      name: typeof obj?.name === "string" ? obj.name : undefined,
      email: typeof obj?.email === "string" ? obj.email : undefined,
      userAgent: typeof obj?.userAgent === "string" ? obj.userAgent : undefined,
      url: typeof obj?.url === "string" ? obj.url : undefined,
    };
  } catch {
    return {};
  }
}

function roomStatusFromBackend(rawState: unknown): Room["status"] {
  const state = String(rawState ?? "active").trim().toLowerCase();
  if (state === "closed" || state === "resolved") return "resolved";
  return "assigned";
}

function toIsoDate(raw: unknown, fallback = new Date().toISOString()) {
  if (!raw) return fallback;
  const date = new Date(String(raw));
  return Number.isNaN(date.getTime()) ? fallback : date.toISOString();
}

function normalizeStartedRoom(raw: any): { room: Room; previousRequestId?: string } | null {
  if (!raw || typeof raw !== "object") return null;
  const hasBackendRoomShape =
    field(raw, "clientRequestId") !== undefined ||
    field(raw, "assignedAgentId") !== undefined ||
    field(raw, "agentId") !== undefined ||
    field(raw, "agentChannel") !== undefined ||
    field(raw, "customerConnectionId") !== undefined ||
    field(raw, "clientConnectionId") !== undefined ||
    field(raw, "roomStatus") !== undefined;
  if (!hasBackendRoomShape) return null;

  const session = useSessionStore.getState();
  const now = new Date().toISOString();
  const id = String(raw?._id ?? field(raw, "id") ?? field(raw, "roomId") ?? "");
  if (!id) return null;

  const previousRequestId = String(field(raw, "clientRequestId") ?? field(raw, "requestId") ?? "");
  const fallback = previousRequestId ? useRoomStore.getState().byId[previousRequestId] : undefined;
  const clientInfo = parseClientInfo(field<string>(raw, "clientInfo"));
  const department = field<{ id?: string | null; name?: string | null } | null>(raw, "department");
  const customerId = fallback?.customerId || clientInfo.email || String(field(raw, "userId") ?? field(raw, "clientId") ?? field(raw, "contactId") ?? id);
  const lang = String(field(raw, "lang") ?? fallback?.language ?? "en");
  const createdAt = toIsoDate(field(raw, "created") ?? field(raw, "createdAt"), fallback?.createdAt ?? now);
  const assignedAgentId = String(field(raw, "assignedAgentId") ?? field(raw, "agentId") ?? fallback?.assignedAgentId ?? session.agentId);

  if (assignedAgentId && session.agentId !== assignedAgentId) {
    useSessionStore.getState().setSession({ agentId: assignedAgentId });
  }

  if (!useCustomerStore.getState().byId[customerId]) {
    useCustomerStore.setState((state) => ({
      byId: {
        ...state.byId,
        [customerId]: {
          id: customerId,
          name: clientInfo.name || clientInfo.email || fallback?.customerId || "Guest",
          identities: [],
          language: lang,
          email: clientInfo.email,
          tags: [],
        },
      },
    }));
  }

  return {
    previousRequestId: previousRequestId || undefined,
    room: {
      ...(fallback ?? {}),
      id,
      tenantId: fallback?.tenantId ?? session.tenantId,
      customerId,
      channel: normalizeChannel(String(field(raw, "channel") ?? fallback?.channel ?? "web_chat")),
      channelAccountId: fallback?.channelAccountId ?? String(field(raw, "agentChannel") ?? ""),
      channelRoomId: fallback?.channelRoomId ?? String(field(raw, "customerConnectionId") ?? field(raw, "clientConnectionId") ?? ""),
      queueId: fallback?.queueId ?? department?.id ?? "",
      assignedAgentId,
      status: roomStatusFromBackend(field(raw, "roomStatus") ?? field(raw, "state") ?? fallback?.status),
      priority: fallback?.priority ?? "normal",
      language: lang,
      sentiment: fallback?.sentiment ?? "neutral",
      tags: fallback?.tags ?? [],
      createdAt,
      updatedAt: now,
      lastMessageAt: fallback?.lastMessageAt ?? createdAt,
      sla: fallback?.sla ?? { state: "ok" },
      unreadCount: fallback?.unreadCount ?? 0,
      lastMessagePreview: fallback?.lastMessagePreview ?? "",
      botHandled: fallback?.botHandled ?? false,
      humanHandled: true,
      version: fallback?.version ?? 0,
      sequenceNumber: fallback?.sequenceNumber ?? 0,
      participants: fallback?.participants ?? [],
      offerExpiresAt: undefined,
    },
  };
}

function normalizeBackendMessage(raw: any, room?: Room): Message | null {
  if (!raw || typeof raw !== "object") return null;
  const roomId = String(field(raw, "roomId") ?? room?.id ?? "");
  if (!roomId) return null;
  const rawSenderType = String(field(raw, "senderType") ?? field(raw, "direction") ?? "customer").toLowerCase();
  const senderType: Message["senderType"] =
    rawSenderType === "outbound" ? "agent" : rawSenderType === "inbound" ? "customer" : (rawSenderType as Message["senderType"]);
  return {
    ...(raw as Message),
    id: String(field(raw, "id") ?? `msg-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`),
    roomId,
    tenantId: room?.tenantId ?? "",
    senderId: String(field(raw, "senderId") ?? ""),
    senderType,
    channel: normalizeChannel(String(field(raw, "channel") ?? room?.channel ?? "web_chat")),
    type: "text",
    text: String(field(raw, "text") ?? ""),
    status: "sent",
    sentAt: toIsoDate(field(raw, "timestamp") ?? field(raw, "sentAt")),
    sequenceNumber: Number(field(raw, "sequenceNumber") ?? 0),
  };
}

export function wireDispatcher(client: IRealtimeClient): Unsubscribe {
  const offs: Unsubscribe[] = [];

  offs.push(
    client.onConnectionStateChanged((state) => {
      useRealtimeStore.getState().setConnectionState(state);
    }),
  );

  offs.push(
    client.on<any>("ActiveRooms", (env) => {
      const raw = env.payload as any;
      const list: any[] = Array.isArray(raw) ? raw : Array.isArray(raw?.rooms) ? raw.rooms : [];
      if (list.length === 0) return;

      const convStore = useRoomStore.getState();
      const msgStore = useMessageStore.getState();
      const hadOpenTabs = convStore.openTabs.length > 0;
      let firstId: string | null = null;

      for (const item of list) {
        const started = normalizeStartedRoom(item);
        if (!started) continue;
        const conv = started.room;
        convStore.upsertRoom(conv);
        firstId ??= conv.id;

        const rawMessages: any[] =
          (field(item, "messages") as any[]) ?? (field(item, "Messages") as any[]) ?? [];
        if (Array.isArray(rawMessages) && rawMessages.length > 0) {
          const msgs = rawMessages
            .map((m) => normalizeBackendMessage({ ...m, roomId: conv.id }, conv))
            .filter((m): m is Message => m !== null)
            .sort((a, b) => +new Date(a.sentAt) - +new Date(b.sentAt));
          msgStore.setMessages(conv.id, msgs);
          const last = msgs[msgs.length - 1];
          if (last) useRoomStore.getState().touchLastMessage(conv.id, last);
        }
      }

      if (!hadOpenTabs && firstId) {
        useRoomStore.getState().openTab(firstId);
        useInboxStore.getState().setView("assigned_me");
      }
    }),
  );


  offs.push(
    client.on<Message>("MessageReceived", (env) => {
      const raw = env.payload as any;
      const roomId = String(field(raw, "roomId") ?? env.roomId ?? "");
      const room = roomId ? useRoomStore.getState().byId[roomId] : undefined;
      const rawSenderType = String(field(raw, "senderType") ?? field(raw, "direction") ?? "customer").toLowerCase();
      const msg: Message = {
        ...(raw as Message),
        id: String(field(raw, "id") ?? `msg-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`),
        roomId,
        tenantId: room?.tenantId ?? "",
        senderId: String(field(raw, "senderId") ?? ""),
        senderType: rawSenderType === "outbound" ? "agent" : rawSenderType === "inbound" ? "customer" : (rawSenderType as Message["senderType"]),
        channel: normalizeChannel(String(field(raw, "channel") ?? room?.channel ?? "web_chat")),
        type: "text",
        text: String(field(raw, "text") ?? ""),
        status: "sent",
        sentAt: toIsoDate(field(raw, "timestamp") ?? field(raw, "sentAt")),
        sequenceNumber: Number(field(raw, "sequenceNumber") ?? 0),
      };
      if (!roomId) return;
      useMessageStore.getState().upsertMessage(msg);
      useRoomStore.getState().touchLastMessage(msg.roomId, msg);
    }),
  );

  offs.push(
    client.on<{ clientCorrelationId?: string; serverId: string; sentAt: string; status: "sent" }>(
      "MessageAcknowledged",
      (env) => {
        const { clientCorrelationId, serverId } = env.payload;
        if (!clientCorrelationId || !env.roomId) return;
        useMessageStore
          .getState()
          .acknowledgeMessage(env.roomId, clientCorrelationId, serverId, "sent");
      },
    ),
  );

  offs.push(
    client.on<{ clientCorrelationId?: string; messageId?: string; status: Message["status"] }>(
      "MessageStatusChanged",
      (env) => {
        if (!env.roomId) return;
        useMessageStore
          .getState()
          .setMessageStatus(env.roomId, env.payload.clientCorrelationId ?? env.payload.messageId ?? "", env.payload.status);
      },
    ),
  );

  offs.push(
    client.on<{ roomId: string; isTyping: boolean; who: "customer" | "agent" }>(
      "TypingChanged",
      (env) => {
        const { roomId, isTyping, who } = env.payload;
        useRoomStore.getState().setTyping(roomId, who, isTyping);
      },
    ),
  );

  offs.push(
    client.on<RequestOfferedPayload>("RoomOffered", (env) => {
      const p = env.payload;
      if (!p?.requestId) return;
      const info = parseClientInfo(p.clientInfo);
      const now = new Date().toISOString();
      const customerId = info.email || p.requestId;
      const lang = p.lang || "en";

      const customerStore = useCustomerStore.getState();
      if (!customerStore.byId[customerId]) {
        const customer: Customer = {
          id: customerId,
          name: info.name || info.email || "Guest",
          identities: [],
          language: lang,
          email: info.email,
          tags: [],
        };
        useCustomerStore.setState({ byId: { ...customerStore.byId, [customerId]: customer } });
      }

      const room: Room = {
        id: p.requestId,
        tenantId: "",
        customerId,
        channel: normalizeChannel(p.channel),
        channelAccountId: "",
        channelRoomId: "",
        queueId: p.department?.id ?? "",
        status: "offered",
        priority: "normal",
        language: lang,
        sentiment: "neutral",
        tags: [],
        createdAt: now,
        updatedAt: now,
        lastMessageAt: now,
        sla: { state: "ok" },
        unreadCount: 0,
        lastMessagePreview: "",
        botHandled: false,
        humanHandled: false,
        version: 0,
        sequenceNumber: 0,
        participants: [],
        offerExpiresAt: new Date(Date.now() + 20_000).toISOString(),
      };
      useRoomStore.getState().upsertRoom(room);
      // Surface the offer: switch inbox to the "Offered to me" view so the
      // Accept/Reject affordance is immediately visible.
      useInboxStore.getState().setView("offered_me");
    }),

  );



  offs.push(
    client.on<Room>("RoomUpdated", (env) => {
      const started = normalizeStartedRoom(env.payload);
      if (started) {
        if (started.previousRequestId && started.previousRequestId !== started.room.id) {
          useRoomStore.getState().removeRoom(started.previousRequestId);
        }
        useRoomStore.getState().upsertRoom(started.room);
        useRoomStore.getState().openTab(started.room.id);
        useInboxStore.getState().setView("assigned_me");
        return;
      }

      const raw = env.payload as Partial<Room> & { roomId?: string; requestId?: string };
      const id = raw.id ?? raw.roomId ?? raw.requestId ?? env.roomId;
      if (!id) return;
      const existing = useRoomStore.getState().byId[id];
      if (!existing && (!raw.customerId || !raw.channel)) return;
      const now = new Date().toISOString();
      const session = useSessionStore.getState();
      const fallback: Room = existing ?? {
        id,
        tenantId: session.tenantId,
        customerId: raw.customerId!,
        channel: normalizeChannel(raw.channel as unknown as string),
        channelAccountId: "",
        channelRoomId: "",
        queueId: raw.queueId ?? "",
        status: "assigned",
        priority: "normal",
        language: "en",
        sentiment: "neutral",
        tags: [],
        createdAt: now,
        updatedAt: now,
        lastMessageAt: now,
        sla: { state: "ok" },
        unreadCount: 0,
        lastMessagePreview: "",
        botHandled: false,
        humanHandled: true,
        version: 0,
        sequenceNumber: 0,
        participants: [],
      };
      const room: Room = {
        ...fallback,
        ...(raw as Room),
        id,
        channel: normalizeChannel((raw.channel ?? fallback.channel) as unknown as string),
        status: raw.status ?? (fallback.status === "offered" ? "assigned" : fallback.status),
        assignedAgentId: raw.assignedAgentId ?? fallback.assignedAgentId ?? session.agentId,
        humanHandled: raw.humanHandled ?? fallback.humanHandled ?? true,
        updatedAt: raw.updatedAt ?? now,
      };
      useRoomStore.getState().upsertRoom(room);
    }),
  );

  offs.push(
    client.on<{ roomId: string; typeOfClose?: string }>(
      "RoomResolved",
      (env) => {
        const id = env.roomId ?? (env.payload as any)?.roomId;
        if (!id) return;
        useRoomStore.getState().setStatus(id, "resolved");
      },
    ),
  );

  return () => offs.forEach((off) => off());
}
