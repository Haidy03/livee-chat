import type { ConnectionState, RealtimeEnvelope } from "@/features/digital-workspace/models";

export type ConceptualEvent =
  | "ActiveRooms"
  | "RoomCreated"
  | "RoomOffered"
  | "RoomUpdated"
  | "RoomRemoved"
  | "RoomAssigned"
  | "RoomTransferred"
  | "RoomStatusChanged"
  | "RoomResolved"
  | "MessageReceived"
  | "MessageAcknowledged"
  | "MessageStatusChanged"
  | "MessageDeleted"
  | "TypingChanged"
  | "ReadReceiptUpdated"
  | "InternalNoteAdded"
  | "CustomerProfileUpdated"
  | "QueueCountsUpdated"
  | "QueueStatisticsUpdated"
  | "AgentPresenceChanged"
  | "AgentCapacityChanged"
  | "SLAUpdated"
  | "SupervisorJoined"
  | "ConnectionStateChanged";

export type ConceptualCommand =
  | "JoinTenant"
  | "SubscribeAgent"
  | "SubscribeRoom"
  | "UnsubscribeRoom"
  | "AcceptRoom"
  | "RejectRoom"
  | "SendMessage"
  | "SendAttachment"
  | "RetryMessage"
  | "SendTyping"
  | "MarkRoomRead"
  | "AssignRoom"
  | "TransferRoom"
  | "UpdateRoomStatus"
  | "AddInternalNote"
  | "UpdateCustomer"
  | "AddTags"
  | "RemoveTags"
  | "ResolveRoom"
  | "ReopenRoom"
  | "SetAgentPresence"
  | "SnoozeRoom";

export type Unsubscribe = () => void;

export interface ConnectOptions {
  url?: string;
  tenantId: string;
  agentId: string;
  token?: string;
}

export interface IRealtimeClient {
  connect(opts: ConnectOptions): Promise<void>;
  disconnect(): Promise<void>;
  reconnect(): Promise<void>;
  on<T = unknown>(event: ConceptualEvent, handler: (env: RealtimeEnvelope<T>) => void): Unsubscribe;
  off(event: ConceptualEvent, handler: (env: RealtimeEnvelope) => void): void;
  invoke<R = void>(command: ConceptualCommand, payload: unknown): Promise<R>;
  subscribeToAgent(agentId: string): Promise<void>;
  subscribeToTenant(tenantId: string): Promise<void>;
  subscribeToRoom(roomId: string): Promise<void>;
  unsubscribeFromRoom(roomId: string): Promise<void>;
  getConnectionState(): ConnectionState;
  onConnectionStateChanged(handler: (state: ConnectionState) => void): Unsubscribe;
}
