// Maps conceptual event/command names to the wire-level SignalR hub method names.
// EDIT THIS FILE when wiring to a different hub — no other code changes required.

import type { ConceptualCommand, ConceptualEvent } from "./IRealtimeClient";

// AgentHub push events (Contact-Center.Api / VoiceFlow.Api/LiveChat/Hubs/AgentHub.cs).
// Names match RoomService / RoutingEngine `SendAsync(...)` calls.
export const SERVER_TO_CLIENT_EVENTS: Record<ConceptualEvent, string> = {
  ActiveRooms: "ActiveRooms",
  RoomCreated: "RoomCreated",
  RoomOffered: "RequestOffered",
  RoomUpdated: "RoomStarted",
  RoomRemoved: "RoomRemoved",
  RoomAssigned: "RoomAssigned",
  RoomTransferred: "RoomTransferred",
  RoomStatusChanged: "RoomStatusChanged",
  RoomResolved: "RoomClosed",
  MessageReceived: "MessageReceived",
  MessageAcknowledged: "MessageAcknowledged",
  MessageStatusChanged: "MessageStatusChanged",
  MessageDeleted: "MessageDeleted",
  TypingChanged: "TypingIndicator",
  ReadReceiptUpdated: "ReadReceiptUpdated",
  InternalNoteAdded: "InternalNoteAdded",
  CustomerProfileUpdated: "CustomerProfileUpdated",
  QueueCountsUpdated: "QueueCountsUpdated",
  QueueStatisticsUpdated: "QueueStatisticsUpdated",
  AgentPresenceChanged: "AgentPresenceChanged",
  AgentCapacityChanged: "AgentCapacityChanged",
  SLAUpdated: "SlaUpdated",
  SupervisorJoined: "SupervisorJoined",
  ConnectionStateChanged: "ConnectionStateChanged",
};

// AgentHub invokable methods (client -> server).
export const CLIENT_TO_SERVER_COMMANDS: Record<ConceptualCommand, string> = {
  JoinTenant: "JoinTenant",
  SubscribeAgent: "SubscribeAgent",
  SubscribeRoom: "SubscribeRoom",
  UnsubscribeRoom: "UnsubscribeRoom",
  AcceptRoom: "AcceptRequest",
  RejectRoom: "DeclineRequest",
  SendMessage: "SendMessage",
  SendAttachment: "SendAttachment",
  RetryMessage: "SendMessage",
  SendTyping: "Typing",
  MarkRoomRead: "MarkRoomRead",
  AssignRoom: "AssignRoom",
  TransferRoom: "TransferRoom",
  UpdateRoomStatus: "UpdateRoomStatus",
  AddInternalNote: "AddInternalNote",
  UpdateCustomer: "UpdateCustomer",
  AddTags: "AddTags",
  RemoveTags: "RemoveTags",
  ResolveRoom: "CloseRoom",
  ReopenRoom: "ReopenRoom",
  SetAgentPresence: "SetStatus",
  SnoozeRoom: "SnoozeRoom",
};
