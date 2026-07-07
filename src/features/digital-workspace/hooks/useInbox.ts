import { useInboxStore, useRoomStore, useSessionStore } from "../stores";
import type { Room, InboxView } from "../models";

export type InboxViewId = InboxView["id"];

const filterForView = (view: InboxViewId, me: string) => {
  const filters: Record<InboxViewId, (c: Room) => boolean> = {
    assigned_me: (c) => c.assignedAgentId === me && !["resolved", "closed", "spam"].includes(c.status),
    offered_me: (c) => c.status === "offered",
    unassigned: (c) => !c.assignedAgentId && !["resolved", "closed", "spam"].includes(c.status),
    team: (c) => !!c.assignedAgentId && c.assignedAgentId !== me && !["resolved", "closed"].includes(c.status),
    waiting_customer: (c) => c.status === "waiting_customer",
    pending: (c) => c.status === "pending",
    escalated: (c) => c.status === "escalated",
    mentions: () => false,
    snoozed: (c) => c.status === "snoozed",
    resolved: (c) => c.status === "resolved",
    all: () => true,
  };
  return filters[view];
};

const INBOX_VIEW_IDS: InboxViewId[] = [
  "assigned_me",
  "offered_me",
  "unassigned",
  "team",
  "waiting_customer",
  "pending",
  "escalated",
  "mentions",
  "snoozed",
  "resolved",
  "all",
];

export function useInboxRooms() {
  const view = useInboxStore((s) => s.activeView);
  const filter = useInboxStore((s) => s.filter);
  const sort = useInboxStore((s) => s.sort);
  const byId = useRoomStore((s) => s.byId);
  const order = useRoomStore((s) => s.order);
  const me = useSessionStore((s) => s.agentId || "agent-me");

  let list = order.map((id) => byId[id]).filter(Boolean);
  list = list.filter(filterForView(view, me));

  if (filter.search.trim()) {
    const q = filter.search.toLowerCase();
    list = list.filter(
      (c) =>
        c.id.toLowerCase().includes(q) ||
        c.subject?.toLowerCase().includes(q) ||
        c.lastMessagePreview.toLowerCase().includes(q) ||
        c.tags.some((t) => t.toLowerCase().includes(q)),
    );
  }
  if (filter.channels.length) list = list.filter((c) => filter.channels.includes(c.channel));
  if (filter.statuses.length) list = list.filter((c) => filter.statuses.includes(c.status));
  if (filter.queues.length) list = list.filter((c) => filter.queues.includes(c.queueId));
  if (filter.priorities.length) list = list.filter((c) => filter.priorities.includes(c.priority));
  if (filter.unreadOnly) list = list.filter((c) => c.unreadCount > 0);

  switch (sort) {
    case "newest":
      list.sort((a, b) => +new Date(b.lastMessageAt) - +new Date(a.lastMessageAt));
      break;
    case "oldest":
      list.sort((a, b) => +new Date(a.lastMessageAt) - +new Date(b.lastMessageAt));
      break;
    case "longest_waiting":
      list.sort(
        (a, b) => +new Date(a.customerWaitingSince ?? a.createdAt) - +new Date(b.customerWaitingSince ?? b.createdAt),
      );
      break;
    case "sla":
      list.sort(
        (a, b) =>
          +new Date(a.sla.firstResponseDeadline ?? a.sla.resolutionDeadline ?? 0) -
          +new Date(b.sla.firstResponseDeadline ?? b.sla.resolutionDeadline ?? 0),
      );
      break;
    case "priority": {
      const w = { urgent: 0, high: 1, normal: 2, low: 3 } as const;
      list.sort((a, b) => w[a.priority] - w[b.priority]);
      break;
    }
    case "name":
      list.sort((a, b) => a.id.localeCompare(b.id));
      break;
  }
  return list;
}

export function getInboxCounts(byId: Record<string, Room>): Record<InboxViewId, number> {
  const me = useSessionStore.getState().agentId || "agent-me";
  const out: Record<InboxViewId, number> = {
    assigned_me: 0,
    offered_me: 0,
    unassigned: 0,
    team: 0,
    waiting_customer: 0,
    pending: 0,
    escalated: 0,
    mentions: 0,
    snoozed: 0,
    resolved: 0,
    all: 0,
  };
  Object.values(byId).forEach((c) => {
    INBOX_VIEW_IDS.forEach((k) => {
      if (filterForView(k, me)(c)) out[k]++;
    });
  });
  return out;
}
