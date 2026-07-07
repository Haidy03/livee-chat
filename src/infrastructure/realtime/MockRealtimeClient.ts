import type { ConnectionState, Message, RealtimeEnvelope } from "@/features/digital-workspace/models";
import type {
  ConceptualCommand,
  ConceptualEvent,
  ConnectOptions,
  IRealtimeClient,
  Unsubscribe,
} from "./IRealtimeClient";

type Handler = (env: RealtimeEnvelope) => void;

/**
 * Pure in-memory realtime client. Backed by the mock seed; emits scripted
 * background events and replays acks for optimistic messages.
 * Components NEVER import this directly — they go through RealtimeEventDispatcher.
 */
export class MockRealtimeClient implements IRealtimeClient {
  private handlers = new Map<ConceptualEvent, Set<Handler>>();
  private connectionState: ConnectionState = "disconnected";
  private connectionHandlers = new Set<(s: ConnectionState) => void>();
  private tenantId = "";
  private agentId = "";
  private timers: number[] = [];
  private subscribedRooms = new Set<string>();

  async connect(opts: ConnectOptions) {
    this.tenantId = opts.tenantId;
    this.agentId = opts.agentId;
    this.setConnectionState("connecting");
    await this.delay(180);
    this.setConnectionState("connected");
    this.startBackgroundLoops();
  }

  async disconnect() {
    this.stopBackgroundLoops();
    this.setConnectionState("disconnected");
  }

  async reconnect() {
    this.stopBackgroundLoops();
    this.setConnectionState("reconnecting");
    await this.delay(800);
    this.setConnectionState("connected");
    this.startBackgroundLoops();
  }

  on<T = unknown>(event: ConceptualEvent, handler: (env: RealtimeEnvelope<T>) => void): Unsubscribe {
    let set = this.handlers.get(event);
    if (!set) {
      set = new Set();
      this.handlers.set(event, set);
    }
    set.add(handler as Handler);
    return () => set!.delete(handler as Handler);
  }

  off(event: ConceptualEvent, handler: Handler) {
    this.handlers.get(event)?.delete(handler);
  }

  async invoke<R = void>(command: ConceptualCommand, payload: unknown): Promise<R> {
    // Simulate latency + ack for messaging commands.
    await this.delay(200 + Math.random() * 600);

    if (command === "SendMessage") {
      const msg = payload as Partial<Message>;
      this.emit("MessageAcknowledged", {
        event: "MessageAcknowledged",
        tenantId: this.tenantId,
        roomId: msg.roomId,
        serverSentAt: new Date().toISOString(),
        payload: {
          clientCorrelationId: msg.clientCorrelationId,
          serverId: `srv-${Math.random().toString(36).slice(2, 10)}`,
          sentAt: new Date().toISOString(),
          status: "sent",
        },
      });
      // Simulate delivered + read shortly after.
      setTimeout(() => {
        this.emit("MessageStatusChanged", {
          event: "MessageStatusChanged",
          tenantId: this.tenantId,
          roomId: msg.roomId,
          serverSentAt: new Date().toISOString(),
          payload: { clientCorrelationId: msg.clientCorrelationId, status: "delivered" },
        });
      }, 800);
      setTimeout(() => {
        this.emit("MessageStatusChanged", {
          event: "MessageStatusChanged",
          tenantId: this.tenantId,
          roomId: msg.roomId,
          serverSentAt: new Date().toISOString(),
          payload: { clientCorrelationId: msg.clientCorrelationId, status: "read" },
        });
      }, 2200);
    }

    return undefined as R;
  }

  async subscribeToAgent(_agentId: string) {}
  async subscribeToTenant(_tenantId: string) {}
  async subscribeToRoom(roomId: string) {
    this.subscribedRooms.add(roomId);
  }
  async unsubscribeFromRoom(roomId: string) {
    this.subscribedRooms.delete(roomId);
  }

  getConnectionState() {
    return this.connectionState;
  }
  onConnectionStateChanged(handler: (s: ConnectionState) => void): Unsubscribe {
    this.connectionHandlers.add(handler);
    return () => this.connectionHandlers.delete(handler);
  }

  // ---------- public helpers for the Dev Mock Panel ----------
  emit<T = unknown>(event: ConceptualEvent, env: RealtimeEnvelope<T>) {
    this.handlers.get(event)?.forEach((h) => {
      try {
        h(env as RealtimeEnvelope);
      } catch (e) {
        console.error("[MockRealtime] handler error", e);
      }
    });
  }

  simulateDisconnect() {
    this.stopBackgroundLoops();
    this.setConnectionState("disconnected");
  }

  // ---------- internals ----------
  private setConnectionState(s: ConnectionState) {
    this.connectionState = s;
    this.connectionHandlers.forEach((h) => h(s));
  }

  private startBackgroundLoops() {
    this.stopBackgroundLoops();
    // SLA ticks every second
    this.timers.push(
      window.setInterval(() => {
        this.emit("SLAUpdated", {
          event: "SLAUpdated",
          tenantId: this.tenantId,
          serverSentAt: new Date().toISOString(),
          payload: { at: Date.now() },
        });
      }, 1000) as unknown as number,
    );
    // Queue counts every 15s
    this.timers.push(
      window.setInterval(() => {
        this.emit("QueueCountsUpdated", {
          event: "QueueCountsUpdated",
          tenantId: this.tenantId,
          serverSentAt: new Date().toISOString(),
          payload: { drift: Math.floor(Math.random() * 3) - 1 },
        });
      }, 15000) as unknown as number,
    );
  }

  private stopBackgroundLoops() {
    this.timers.forEach((t) => clearInterval(t));
    this.timers = [];
  }

  private delay(ms: number) {
    return new Promise<void>((r) => setTimeout(r, ms));
  }
}

// Singleton for the mock dev panel to reach.
let _mock: MockRealtimeClient | null = null;
export function getMockClient() {
  if (!_mock) _mock = new MockRealtimeClient();
  return _mock;
}
