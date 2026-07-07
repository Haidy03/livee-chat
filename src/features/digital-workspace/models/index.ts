// All Digital Workspace TypeScript models. Kept in one file for cohesion.
// Server wire-shape will be mapped here at the realtime/api boundary.

export type Channel =
  | "web_chat"
  | "mobile_app"
  | "whatsapp"
  | "messenger"
  | "instagram_dm"
  | "instagram_comment"
  | "facebook_comment"
  | "twitter_dm"
  | "twitter_mention"
  | "telegram";

export const ALL_CHANNELS: Channel[] = [
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

export type AgentPresence =
  | "available"
  | "busy"
  | "away"
  | "break"
  | "offline"
  | "after_contact_work";

export type ChannelGroup = "phone" | "chat" | "email" | "social";

export const CHANNEL_GROUPS: ChannelGroup[] = ["phone", "chat", "email", "social"];

export const DEFAULT_CHANNEL_AVAILABILITY: Record<ChannelGroup, boolean> = {
  phone: true,
  chat: true,
  email: true,
  social: true,
};

export interface Agent {
  id: string;
  name: string;
  email: string;
  avatarUrl?: string;
  presence: AgentPresence;
  capacity: { current: number; max: number };
  skills: string[];
  languages: string[];
  team?: string;
  role?: "agent" | "supervisor" | "admin";
  channelAvailability: Record<ChannelGroup, boolean>;
}

export interface Queue {
  id: string;
  name: string;
  group: string; // e.g. "Customer Service"
  channels: Channel[];
  requiredSkills: string[];
  waiting: number;
  activeAgents: number;
  expectedWaitSec?: number;
}

export interface CustomerIdentity {
  id: string;
  channel: Channel;
  handle: string; // phone, email, social handle, visitor id
  verified?: boolean;
  display?: string;
}

export interface Customer {
  id: string;
  name: string;
  avatarUrl?: string;
  identities: CustomerIdentity[];
  language: string;
  country?: string;
  timezone?: string;
  segment?: string;
  vip?: boolean;
  authenticated?: boolean;
  email?: string;
  phone?: string;
  firstContactAt?: string;
  lastSeenAt?: string;
  tags: string[];
  customFields?: Record<string, string>;
  consent?: { marketing: boolean; recording: boolean };
}

export type RoomStatus =
  | "new"
  | "offered"
  | "assigned"
  | "active"
  | "waiting_customer"
  | "pending"
  | "on_hold"
  | "escalated"
  | "snoozed"
  | "resolved"
  | "closed"
  | "spam";

export type Priority = "low" | "normal" | "high" | "urgent";
export type Sentiment = "positive" | "neutral" | "negative";

export interface RoomSLA {
  firstResponseDeadline?: string; // ISO
  resolutionDeadline?: string;
  state: "ok" | "warning" | "breached";
}

export interface Room {
  id: string;
  tenantId: string;
  customerId: string;
  channel: Channel;
  channelAccountId: string;
  channelRoomId: string;
  queueId: string;
  assignedAgentId?: string;
  status: RoomStatus;
  priority: Priority;
  language: string;
  sentiment: Sentiment;
  subject?: string;
  tags: string[];
  caseId?: string;
  createdAt: string;
  updatedAt: string;
  lastMessageAt: string;
  firstResponseAt?: string;
  resolvedAt?: string;
  customerWaitingSince?: string;
  sla: RoomSLA;
  unreadCount: number;
  lastMessagePreview: string;
  botHandled: boolean;
  humanHandled: boolean;
  version: number;
  sequenceNumber: number;
  participants: string[]; // agent ids
  customerTyping?: boolean;
  agentTypingId?: string;
  viewingAgentIds?: string[];
  offerExpiresAt?: string; // for status === 'offered'
}

export type MessageType =
  | "text"
  | "internal_note"
  | "system"
  | "image"
  | "video"
  | "audio"
  | "voice_note"
  | "document"
  | "location"
  | "contact_card"
  | "social_comment"
  | "social_reply"
  | "quick_reply"
  | "form"
  | "call_activity"
  | "summary"
  | "assignment_event"
  | "transfer_event"
  | "status_change"
  | "sla_warning";

export type MessageStatus =
  | "sending"
  | "sent"
  | "delivered"
  | "read"
  | "failed";

export type SenderType = "customer" | "agent" | "bot" | "system" | "supervisor";

export interface MessageAttachment {
  id: string;
  kind: "image" | "video" | "audio" | "document" | "voice_note";
  url: string;
  name?: string;
  sizeBytes?: number;
  mime?: string;
  durationSec?: number;
}

export interface Message {
  id: string;
  clientCorrelationId?: string;
  roomId: string;
  tenantId: string;
  senderId: string;
  senderType: SenderType;
  channel: Channel;
  type: MessageType;
  text?: string;
  html?: string;
  attachments?: MessageAttachment[];
  quotedMessageId?: string;
  publicReply?: boolean;
  internal?: boolean;
  status: MessageStatus;
  sentAt: string;
  deliveredAt?: string;
  readAt?: string;
  failedAt?: string;
  failureReason?: string;
  editedAt?: string;
  deletedAt?: string;
  reactions?: { emoji: string; by: string }[];
  metadata?: Record<string, unknown>;
  sequenceNumber: number;
}

export interface CustomerJourneyItem {
  id: string;
  customerId: string;
  channel: Channel;
  at: string;
  queue?: string;
  agentName?: string;
  status: RoomStatus;
  summary: string;
  intent?: string;
  sentiment?: Sentiment;
}

export interface CaseItem {
  id: string;
  subject: string;
  status: "open" | "pending" | "resolved" | "closed";
  priority: Priority;
  ownerId?: string;
  createdAt: string;
  updatedAt: string;
  roomIds: string[];
  sla?: RoomSLA;
}

export interface KnowledgeArticle {
  id: string;
  title: string;
  excerpt: string;
  body: string;
  source: string;
  updatedAt: string;
  tags: string[];
}

export interface AISuggestion {
  id: string;
  kind:
    | "reply"
    | "next_action"
    | "summary"
    | "intent"
    | "sentiment"
    | "knowledge"
    | "similar_case"
    | "compliance"
    | "escalation"
    | "queue_routing"
    | "disposition";
  title: string;
  body: string;
  confidence: number; // 0..1
  source?: string;
  meta?: Record<string, unknown>;
}

export interface InternalNote {
  id: string;
  customerId: string;
  authorId: string;
  body: string;
  pinned?: boolean;
  createdAt: string;
  mentions?: string[];
}

export type ConnectionState =
  | "connected"
  | "connecting"
  | "reconnecting"
  | "disconnected"
  | "failed";

export interface RealtimeEnvelope<T = unknown> {
  event: string;
  tenantId: string;
  roomId?: string;
  sequenceNumber?: number;
  serverSentAt: string;
  payload: T;
}

export interface InboxView {
  id:
    | "assigned_me"
    | "offered_me"
    | "unassigned"
    | "team"
    | "waiting_customer"
    | "pending"
    | "escalated"
    | "mentions"
    | "snoozed"
    | "resolved"
    | "all";
  count: number;
}

// State machine for valid UI transitions.
export const ALLOWED_TRANSITIONS: Record<RoomStatus, RoomStatus[]> = {
  new: ["offered", "assigned", "spam"],
  offered: ["assigned", "new"],
  assigned: ["active", "pending", "escalated"],
  active: ["waiting_customer", "pending", "escalated", "on_hold", "resolved", "snoozed"],
  waiting_customer: ["active", "resolved", "pending"],
  pending: ["active", "resolved"],
  on_hold: ["active", "resolved"],
  escalated: ["active", "resolved"],
  snoozed: ["active"],
  resolved: ["closed", "active"],
  closed: [],
  spam: [],
};

export function canTransition(from: RoomStatus, to: RoomStatus) {
  return ALLOWED_TRANSITIONS[from]?.includes(to) ?? false;
}

/**
 * A conversation is the ordered list of messages that belong to a Room.
 * It is a typed view over messages — not a persisted entity of its own.
 */
export type Conversation = Message[];
