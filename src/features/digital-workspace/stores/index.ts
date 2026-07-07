import { create } from "zustand";
import { persist } from "zustand/middleware";
import type {
  Agent,
  AgentPresence,
  CaseItem,
  ChannelGroup,
  ConnectionState,
  Room,
  RoomStatus,
  Customer,
  CustomerJourneyItem,
  InboxView,
  KnowledgeArticle,
  Message,
  Queue,
} from "@/features/digital-workspace/models";

// -------------------- Session --------------------
interface SessionState {
  tenantId: string;
  agentId: string;
  workspaceId: string;
  setSession: (s: Partial<SessionState>) => void;
}
export const useSessionStore = create<SessionState>((set) => ({
  tenantId: "tenant-demo",
  agentId: "agent-me",
  workspaceId: "workspace-default",
  setSession: (s) => set(s),
}));

// -------------------- Realtime --------------------
interface RealtimeState {
  connectionState: ConnectionState;
  lastEventAt: number;
  setConnectionState: (s: ConnectionState) => void;
}
export const useRealtimeStore = create<RealtimeState>((set) => ({
  connectionState: "disconnected",
  lastEventAt: 0,
  setConnectionState: (connectionState) =>
    set((state) =>
      state.connectionState === connectionState
        ? state
        : { connectionState, lastEventAt: Date.now() },
    ),
}));

// -------------------- Agents --------------------
interface AgentState {
  byId: Record<string, Agent>;
  meId: string;
  setAgents: (list: Agent[]) => void;
  setPresence: (presence: AgentPresence) => void;
  setActiveChannel: (group: ChannelGroup) => void;
  upsertAgent: (a: Agent) => void;
}
export const useAgentStore = create<AgentState>((set, get) => ({
  byId: {},
  meId: "agent-me",
  setAgents: (list) => {
    const byId: Record<string, Agent> = {};
    list.forEach((a) => (byId[a.id] = a));
    set({ byId });
  },
  setPresence: (presence) => {
    const me = get().byId[get().meId];
    if (!me) return;
    set({ byId: { ...get().byId, [me.id]: { ...me, presence } } });
  },
  setActiveChannel: (group) => {
    const me = get().byId[get().meId];
    if (!me) return;
    const next: Record<ChannelGroup, boolean> = {
      phone: false,
      chat: false,
      email: false,
      social: false,
      [group]: true,
    };
    set({
      byId: {
        ...get().byId,
        [me.id]: { ...me, channelAvailability: next },
      },
    });
  },
  upsertAgent: (a) => set({ byId: { ...get().byId, [a.id]: a } }),
}));

// -------------------- Queues --------------------
interface QueueState {
  byId: Record<string, Queue>;
  setQueues: (list: Queue[]) => void;
}
export const useQueueStore = create<QueueState>((set) => ({
  byId: {},
  setQueues: (list) => {
    const byId: Record<string, Queue> = {};
    list.forEach((q) => (byId[q.id] = q));
    set({ byId });
  },
}));

// -------------------- Customers --------------------
interface CustomerState {
  byId: Record<string, Customer>;
  journeyByCustomer: Record<string, CustomerJourneyItem[]>;
  casesByCustomer: Record<string, CaseItem[]>;
  setCustomers: (list: Customer[]) => void;
  setJourney: (customerId: string, items: CustomerJourneyItem[]) => void;
  setCases: (customerId: string, items: CaseItem[]) => void;
  updateCustomer: (id: string, patch: Partial<Customer>) => void;
}
export const useCustomerStore = create<CustomerState>((set, get) => ({
  byId: {},
  journeyByCustomer: {},
  casesByCustomer: {},
  setCustomers: (list) => {
    const byId: Record<string, Customer> = {};
    list.forEach((c) => (byId[c.id] = c));
    set({ byId });
  },
  setJourney: (customerId, items) => set({ journeyByCustomer: { ...get().journeyByCustomer, [customerId]: items } }),
  setCases: (customerId, items) => set({ casesByCustomer: { ...get().casesByCustomer, [customerId]: items } }),
  updateCustomer: (id, patch) => {
    const current = get().byId[id];
    if (!current) return;
    set({ byId: { ...get().byId, [id]: { ...current, ...patch } } });
  },
}));

// -------------------- Rooms --------------------
interface RoomStoreState {
  byId: Record<string, Room>;
  order: string[]; // most-recent first
  selectedId: string | null;
  openTabs: string[]; // multi-room tabs
  setRooms: (list: Room[]) => void;
  upsertRoom: (c: Room) => void;
  select: (id: string | null) => void;
  openTab: (id: string) => void;
  closeTab: (id: string) => void;
  setStatus: (id: string, status: RoomStatus) => void;
  touchLastMessage: (roomId: string, msg: Message) => void;
  setTyping: (roomId: string, who: "customer" | "agent", on: boolean) => void;
  removeRoom: (id: string) => void;
}

function sameRoom(a?: Room, b?: Room) {
  if (!a || !b) return false;
  const keys = new Set([...Object.keys(a), ...Object.keys(b)] as Array<keyof Room>);
  for (const key of keys) {
    const av = a[key];
    const bv = b[key];
    if (Array.isArray(av) || Array.isArray(bv)) {
      if (!Array.isArray(av) || !Array.isArray(bv) || av.length !== bv.length) return false;
      if (av.some((item, index) => item !== bv[index])) return false;
      continue;
    }
    if (typeof av === "object" && av !== null) {
      if (JSON.stringify(av) !== JSON.stringify(bv)) return false;
      continue;
    }
    if (av !== bv) return false;
  }
  return true;
}

export const useRoomStore = create<RoomStoreState>((set, get) => ({
  byId: {},
  order: [],
  selectedId: null,
  openTabs: [],
  setRooms: (list) => {
    const byId: Record<string, Room> = {};
    list.forEach((c) => (byId[c.id] = c));
    const order = [...list]
      .sort((a, b) => +new Date(b.lastMessageAt) - +new Date(a.lastMessageAt))
      .map((c) => c.id);
    set({ byId, order });
  },
  upsertRoom: (c) => {
    const current = get().byId[c.id];
    if (sameRoom(current, c)) return;
    const byId = { ...get().byId, [c.id]: c };
    const order = get().order.includes(c.id) ? get().order : [c.id, ...get().order];
    set({ byId, order });
  },
  select: (id) => {
    const tabs = get().openTabs;
    if (id && !tabs.includes(id)) set({ openTabs: [...tabs, id], selectedId: id });
    else if (get().selectedId !== id) set({ selectedId: id });
  },
  openTab: (id) => {
    const tabs = get().openTabs;
    const hasTab = tabs.includes(id);
    const isSelected = get().selectedId === id;
    if (hasTab && isSelected) return;
    set({
      openTabs: hasTab ? tabs : [...tabs, id],
      selectedId: id,
    });
  },
  closeTab: (id) => {
    const tabs = get().openTabs.filter((t) => t !== id);
    set({ openTabs: tabs, selectedId: get().selectedId === id ? tabs[tabs.length - 1] ?? null : get().selectedId });
  },
  setStatus: (id, status) => {
    const c = get().byId[id];
    if (!c || c.status === status) return;
    set({ byId: { ...get().byId, [id]: { ...c, status, updatedAt: new Date().toISOString() } } });
  },
  touchLastMessage: (roomId, msg) => {
    const c = get().byId[roomId];
    if (!c) return;
    const updated: Room = {
      ...c,
      lastMessageAt: msg.sentAt,
      lastMessagePreview: msg.text?.slice(0, 140) ?? "(attachment)",
      unreadCount: msg.senderType === "customer" ? c.unreadCount + 1 : c.unreadCount,
      updatedAt: msg.sentAt,
    };
    const order = [roomId, ...get().order.filter((x) => x !== roomId)];
    set({ byId: { ...get().byId, [roomId]: updated }, order });
  },
  setTyping: (roomId, who, on) => {
    const c = get().byId[roomId];
    if (!c) return;
    const patch: Partial<Room> =
      who === "customer" ? { customerTyping: on } : { agentTypingId: on ? "other" : undefined };
    set({ byId: { ...get().byId, [roomId]: { ...c, ...patch } } });
  },
  removeRoom: (id) => {
    if (!get().byId[id]) return;
    const { [id]: _, ...rest } = get().byId;
    set({
      byId: rest,
      order: get().order.filter((x) => x !== id),
      openTabs: get().openTabs.filter((x) => x !== id),
      selectedId: get().selectedId === id ? null : get().selectedId,
    });
  },
}));

// -------------------- Messages --------------------
interface MessageState {
  byId: Record<string, Message>;
  byRoom: Record<string, string[]>; // ordered ids
  setMessages: (roomId: string, list: Message[]) => void;
  upsertMessage: (m: Message) => void;
  acknowledgeMessage: (roomId: string, clientCorrelationId: string, serverId: string, status: Message["status"]) => void;
  setMessageStatus: (roomId: string, idOrCorrelation: string, status: Message["status"]) => void;
  removeMessage: (roomId: string, id: string) => void;
}
export const useMessageStore = create<MessageState>((set, get) => ({
  byId: {},
  byRoom: {},
  setMessages: (roomId, list) => {
    const byId = { ...get().byId };
    list.forEach((m) => (byId[m.id] = m));
    set({ byId, byRoom: { ...get().byRoom, [roomId]: list.map((m) => m.id) } });
  },
  upsertMessage: (m) => {
    const byId = { ...get().byId, [m.id]: m };
    const ids = get().byRoom[m.roomId] ?? [];
    const newIds = ids.includes(m.id) ? ids : [...ids, m.id];
    set({ byId, byRoom: { ...get().byRoom, [m.roomId]: newIds } });
  },
  acknowledgeMessage: (roomId, clientCorrelationId, serverId, status) => {
    const ids = get().byRoom[roomId] ?? [];
    const byId = { ...get().byId };
    const oldId = ids.find((id) => byId[id]?.clientCorrelationId === clientCorrelationId);
    if (!oldId) return;
    const old = byId[oldId];
    delete byId[oldId];
    byId[serverId] = { ...old, id: serverId, status };
    set({
      byId,
      byRoom: { ...get().byRoom, [roomId]: ids.map((id) => (id === oldId ? serverId : id)) },
    });
  },
  setMessageStatus: (roomId, idOrCorrelation, status) => {
    const ids = get().byRoom[roomId] ?? [];
    const byId = { ...get().byId };
    const target = ids.find((id) => id === idOrCorrelation || byId[id]?.clientCorrelationId === idOrCorrelation);
    if (!target) return;
    byId[target] = { ...byId[target], status };
    set({ byId });
  },
  removeMessage: (roomId, id) => {
    const { [id]: _, ...byId } = get().byId;
    set({
      byId,
      byRoom: {
        ...get().byRoom,
        [roomId]: (get().byRoom[roomId] ?? []).filter((x) => x !== id),
      },
    });
  },
}));

// -------------------- Inbox & filters --------------------
type FilterFacet = {
  channels: string[];
  statuses: RoomStatus[];
  queues: string[];
  priorities: string[];
  unreadOnly: boolean;
  search: string;
};
interface InboxState {
  activeView: InboxView["id"];
  filter: FilterFacet;
  sort: "newest" | "oldest" | "longest_waiting" | "sla" | "priority" | "name";
  setView: (v: InboxView["id"]) => void;
  setFilter: (f: Partial<FilterFacet>) => void;
  setSort: (s: InboxState["sort"]) => void;
  resetFilter: () => void;
}
const defaultFilter: FilterFacet = {
  channels: [],
  statuses: [],
  queues: [],
  priorities: [],
  unreadOnly: false,
  search: "",
};
export const useInboxStore = create<InboxState>((set) => ({
  activeView: "assigned_me",
  filter: defaultFilter,
  sort: "newest",
  setView: (v) => set((state) => (state.activeView === v ? state : { activeView: v })),
  setFilter: (f) => set((s) => ({ filter: { ...s.filter, ...f } })),
  setSort: (sort) => set((state) => (state.sort === sort ? state : { sort })),
  resetFilter: () => set({ filter: defaultFilter }),
}));

// -------------------- Layout (persisted) --------------------
interface LayoutState {
  inboxNavCollapsed: boolean;
  streamCollapsed: boolean;
  contextCollapsed: boolean;
  panelSizes: { stream: number; workspace: number; context: number };
  pixelWidths: { stream: number; workspace: number; context: number; total: number };
  expandedSizes: { stream: number; workspace: number; context: number } | null;
  activeContextTab: string;
  setInboxNavCollapsed: (v: boolean) => void;
  setStreamCollapsed: (v: boolean) => void;
  setContextCollapsed: (v: boolean) => void;
  setPanelSizes: (s: Partial<LayoutState["panelSizes"]>) => void;
  setPixelWidths: (w: Partial<LayoutState["pixelWidths"]>) => void;
  setExpandedSizes: (s: LayoutState["expandedSizes"]) => void;
  setActiveContextTab: (id: string) => void;
}
export const useLayoutStore = create<LayoutState>()(
  persist(
    (set) => ({
      inboxNavCollapsed: false,
      streamCollapsed: false,
      contextCollapsed: false,
      panelSizes: { stream: 25, workspace: 35, context: 40 },
      pixelWidths: { stream: 0, workspace: 0, context: 0, total: 0 },
      expandedSizes: null,
      activeContextTab: "overview",
      setInboxNavCollapsed: (inboxNavCollapsed) => set({ inboxNavCollapsed }),
      setStreamCollapsed: (streamCollapsed) =>
        set((state) => (state.streamCollapsed === streamCollapsed ? state : { streamCollapsed })),
      setContextCollapsed: (contextCollapsed) =>
        set((state) => (state.contextCollapsed === contextCollapsed ? state : { contextCollapsed })),
      setPanelSizes: (s) =>
        set((state) => {
          const next = { ...state.panelSizes, ...s };
          return next.stream === state.panelSizes.stream &&
            next.workspace === state.panelSizes.workspace &&
            next.context === state.panelSizes.context
            ? state
            : { panelSizes: next };
        }),
      setPixelWidths: (w) =>
        set((state) => {
          const next = { ...state.pixelWidths, ...w };
          return next.stream === state.pixelWidths.stream &&
            next.workspace === state.pixelWidths.workspace &&
            next.context === state.pixelWidths.context &&
            next.total === state.pixelWidths.total
            ? state
            : { pixelWidths: next };
        }),
      setExpandedSizes: (expandedSizes) =>
        set((state) => {
          const current = state.expandedSizes;
          const same =
            current === expandedSizes ||
            (!!current &&
              !!expandedSizes &&
              current.stream === expandedSizes.stream &&
              current.workspace === expandedSizes.workspace &&
              current.context === expandedSizes.context);
          return same ? state : { expandedSizes };
        }),
      setActiveContextTab: (activeContextTab) =>
        set((state) => (state.activeContextTab === activeContextTab ? state : { activeContextTab })),
    }),
    {
      name: "digital.layout",
      version: 2,
      migrate: (persisted: any, version) => {
        if (!persisted) return persisted;
        if (version < 2) {
          return {
            ...persisted,
            panelSizes: { stream: 25, workspace: 35, context: 40 },
            expandedSizes: null,
            pixelWidths: { stream: 0, workspace: 0, context: 0, total: 0 },
          };
        }
        return persisted;
      },
    },
  ),
);

// -------------------- Preferences (persisted) --------------------
interface PreferencesState {
  sendOnEnter: boolean;
  desktopNotifications: boolean;
  notificationSound: boolean;
  autoOpenNew: boolean;
  autoOpenNext: boolean;
  compactList: boolean;
  showTranslation: boolean;
  showAIAssist: boolean;
  showJourney: boolean;
  drafts: Record<string, string>;
  setPreference: <K extends keyof PreferencesState>(k: K, v: PreferencesState[K]) => void;
  setDraft: (roomId: string, text: string) => void;
  clearDraft: (roomId: string) => void;
}
export const usePreferencesStore = create<PreferencesState>()(
  persist(
    (set, get) => ({
      sendOnEnter: true,
      desktopNotifications: true,
      notificationSound: true,
      autoOpenNew: false,
      autoOpenNext: true,
      compactList: false,
      showTranslation: true,
      showAIAssist: true,
      showJourney: true,
      drafts: {},
      setPreference: (k, v) => set({ [k]: v } as Partial<PreferencesState>),
      setDraft: (id, text) => set({ drafts: { ...get().drafts, [id]: text } }),
      clearDraft: (id) => {
        const { [id]: _, ...rest } = get().drafts;
        set({ drafts: rest });
      },
    }),
    { name: "digital.prefs" },
  ),
);

// -------------------- Knowledge --------------------
interface KnowledgeState {
  articles: KnowledgeArticle[];
  setArticles: (a: KnowledgeArticle[]) => void;
}
export const useKnowledgeStore = create<KnowledgeState>((set) => ({
  articles: [],
  setArticles: (articles) => set({ articles }),
}));

// -------------------- Notifications --------------------
interface NotificationItem {
  id: string;
  title: string;
  body?: string;
  kind: "info" | "success" | "warning" | "error";
  createdAt: number;
}
interface NotificationState {
  items: NotificationItem[];
  push: (n: Omit<NotificationItem, "id" | "createdAt">) => void;
  clear: () => void;
}
export const useNotificationStore = create<NotificationState>((set, get) => ({
  items: [],
  push: (n) =>
    set({
      items: [
        ...get().items,
        { ...n, id: `n-${Math.random().toString(36).slice(2, 9)}`, createdAt: Date.now() },
      ],
    }),
  clear: () => set({ items: [] }),
}));

// -------------------- LiveChat Agent Config (from GET /Account/GetProjectUser) --------------------
// Timeout fields are stored in SECONDS (API returns minutes; hook converts on hydrate).
interface AgentConfigState {
  loaded: boolean;
  chatSlots: number;
  maxInActiveClient: number;
  maxAgentInActive: number;
  maxAgentInActiveWithClient: number;
  clientDisconnectedTimeout: number;
  agentDisconnectedTimeout: number;
  userAvailable: boolean;
  chattingType: string | null;
  setFromApi: (cfg: {
    ChatSlots: number;
    clientInactiveTimeout: number;
    agentInactiveTimeout: number;
    clientDisconnectedTimeout: number;
    agentDisconnectedTimeout: number;
    user_available: boolean;
    chattingType: string | null;
  }) => void;
}
export const useAgentConfigStore = create<AgentConfigState>((set) => ({
  loaded: false,
  chatSlots: 0,
  maxInActiveClient: 0,
  maxAgentInActive: 0,
  maxAgentInActiveWithClient: 0,
  clientDisconnectedTimeout: 0,
  agentDisconnectedTimeout: 0,
  userAvailable: false,
  chattingType: null,
  setFromApi: (cfg) =>
    set((state) => {
      const next = {
        loaded: true,
        chatSlots: cfg.ChatSlots,
        maxInActiveClient: cfg.clientInactiveTimeout * 60,
        maxAgentInActive: cfg.agentInactiveTimeout * 60,
        maxAgentInActiveWithClient: cfg.agentInactiveTimeout * 60,
        clientDisconnectedTimeout: cfg.clientDisconnectedTimeout * 60,
        agentDisconnectedTimeout: cfg.agentDisconnectedTimeout * 60,
        userAvailable: cfg.user_available,
        chattingType: cfg.chattingType,
      };
      return state.loaded === next.loaded &&
        state.chatSlots === next.chatSlots &&
        state.maxInActiveClient === next.maxInActiveClient &&
        state.maxAgentInActive === next.maxAgentInActive &&
        state.maxAgentInActiveWithClient === next.maxAgentInActiveWithClient &&
        state.clientDisconnectedTimeout === next.clientDisconnectedTimeout &&
        state.agentDisconnectedTimeout === next.agentDisconnectedTimeout &&
        state.userAvailable === next.userAvailable &&
        state.chattingType === next.chattingType
        ? state
        : next;
    }),
}));

// -------------------- LiveChat per-room ephemeral timer spans (seconds) --------------------
export interface LiveChatTimerSpan {
  agentMessageSpan: number; // seconds since last agent message (0 = n/a)
  utteranceSpan: number; // seconds since last customer utterance (0 = n/a)
  offlineSpan: number; // seconds client has been offline (0 = n/a)
  notifyPending: boolean; // notify-before-close has fired
  deleted: boolean;
}
interface LiveChatTimerState {
  byRoom: Record<string, LiveChatTimerSpan>;
  setSpans: (next: Record<string, LiveChatTimerSpan>) => void;
}

function sameLiveChatTimerSpans(
  a: Record<string, LiveChatTimerSpan>,
  b: Record<string, LiveChatTimerSpan>,
) {
  const aKeys = Object.keys(a);
  const bKeys = Object.keys(b);
  if (aKeys.length !== bKeys.length) return false;
  for (const key of bKeys) {
    const left = a[key];
    const right = b[key];
    if (!left ||
      left.agentMessageSpan !== right.agentMessageSpan ||
      left.utteranceSpan !== right.utteranceSpan ||
      left.offlineSpan !== right.offlineSpan ||
      left.notifyPending !== right.notifyPending ||
      left.deleted !== right.deleted
    ) {
      return false;
    }
  }
  return true;
}

export const useLiveChatTimerStore = create<LiveChatTimerState>((set, get) => ({
  byRoom: {},
  setSpans: (next) => {
    const prev = get().byRoom;
    if (prev === next || sameLiveChatTimerSpans(prev, next)) return;
    set({ byRoom: next });
  },
}));
