import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { ILogger, IRetryPolicy, RetryContext } from "@microsoft/signalr";
import type { ConnectionState, RealtimeEnvelope } from "@/features/digital-workspace/models";
import type {
  ConceptualCommand,
  ConceptualEvent,
  ConnectOptions,
  IRealtimeClient,
  Unsubscribe,
} from "./IRealtimeClient";
import {
  CLIENT_TO_SERVER_COMMANDS,
  SERVER_TO_CLIENT_EVENTS,
} from "./realtimeEventMap";

/**
 * SignalR client for the C# AgentHub (`/hubs/agent`).
 * - Token is read from localStorage["vf_access_token"] on every handshake.
 * - Backend hub methods take positional args, so we adapt object payloads
 *   via COMMAND_ADAPTERS below.
 */

const TOKEN_KEY = "vf_access_token";
const DEFAULT_RETRY_DELAYS_MS = [2000, 5000, 15000, 30000, 60000];

const REDACTED_SIGNALR_LOGGER: ILogger = {
  log(logLevel, message) {
    if (logLevel < LogLevel.Information) return;
    const sanitized = message.replace(/([?&]access_token=)[^&\s]+/gi, "$1[redacted]");
    if (logLevel >= LogLevel.Error) console.error(sanitized);
    else if (logLevel >= LogLevel.Warning) console.warn(sanitized);
    else console.info(sanitized);
  },
};

const shouldLogRealtimeDebug = () =>
  Boolean(import.meta.env.DEV || import.meta.env.VITE_REALTIME_DEBUG === "true");

type CommandAdapter = (payload: any) => unknown[];

const COMMAND_ADAPTERS: Partial<Record<ConceptualCommand, CommandAdapter>> = {
  SetAgentPresence: (p) => [String(p?.status ?? p ?? "Available")],
  AcceptRoom: (p) => [String(p?.requestId ?? p?.roomId ?? p)],
  RejectRoom: (p) => [String(p?.requestId ?? p?.roomId ?? p)],
  SendMessage: (p) => [String(p?.roomId ?? ""), String(p?.text ?? "")],
  SendTyping: (p) => [String(p?.roomId ?? ""), Boolean(p?.isTyping)],
  ResolveRoom: (p) => [
    String(p?.roomId ?? ""),
    String(p?.typeOfClose ?? p?.disposition ?? "resolved"),
  ],
};

function toEnvelope<T>(event: ConceptualEvent, tenantId: string, raw: any): RealtimeEnvelope<T> {
  return {
    event,
    tenantId,
    roomId: raw?.roomId ?? raw?.RoomId ?? raw?.id,
    serverSentAt: new Date().toISOString(),
    payload: raw as T,
  };
}

export class CoreSignalRClient implements IRealtimeClient {
  private connection: HubConnection | null = null;
  private tenantId = "";
  private agentId = "";
  private state: ConnectionState = "disconnected";
  private stateHandlers = new Set<(s: ConnectionState) => void>();
  private eventHandlers = new Map<ConceptualEvent, Set<(env: RealtimeEnvelope) => void>>();
  private registered = new Set<ConceptualEvent>();
  private rejectedReason: string | null = null;
  private token?: string;
  private url?: string;

  private readonly reconnectPolicy: IRetryPolicy = {
    nextRetryDelayInMilliseconds: (retryContext: RetryContext) => {
      if (this.rejectedReason) return null;
      return DEFAULT_RETRY_DELAYS_MS[retryContext.previousRetryCount] ?? null;
    },
  };

  private setState(s: ConnectionState) {
    this.state = s;
    this.stateHandlers.forEach((h) => h(s));
  }

  private async announceAvailable(context: "connect" | "reconnect") {
    try {
      await this.connection?.invoke("SetStatus", "Available");
    } catch (err) {
      console.warn("[CoreSignalRClient] SetStatus(Available) failed", {
        context,
        error: (err as Error)?.message,
      });
    }
  }


  async connect(opts: ConnectOptions): Promise<void> {
    if (this.connection) {
      await this.disconnect();
    }

    this.tenantId = opts.tenantId;
    this.agentId = opts.agentId;
    this.token = opts.token;
    this.url = opts.url;

    const baseUrl = (opts.url ?? import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");
    if (!baseUrl) {
      this.setState("disconnected");
      throw new Error("VITE_API_URL is not configured");
    }
    const hubUrl = `${baseUrl}/hubs/agent`;

    this.setState("connecting");
    this.rejectedReason = null;
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () =>
          opts.token ?? localStorage.getItem(TOKEN_KEY) ?? "",
      })
      .withAutomaticReconnect(this.reconnectPolicy)
      .configureLogging(REDACTED_SIGNALR_LOGGER)
      .build();

    // Register lifecycle handlers BEFORE start() so we don't miss the first close.
    this.connection.onclose((err) => {
      console.warn("[CoreSignalRClient] onclose", {
        url: hubUrl,
        rejectedReason: this.rejectedReason,
        error: err?.message ?? err,
      });
      this.setState("disconnected");
    });
    this.connection.onreconnecting((err) => {
      console.warn("[CoreSignalRClient] onreconnecting", {
        url: hubUrl,
        rejectedReason: this.rejectedReason,
        error: err?.message ?? err,
      });
      if (this.rejectedReason) {
        void this.connection?.stop().catch(() => undefined);
        this.setState("disconnected");
        return;
      }
      this.setState("reconnecting");
    });
    this.connection.onreconnected(() => {
      console.info("[CoreSignalRClient] onreconnected", { url: hubUrl });
      this.setState("connected");
      void this.announceAvailable("reconnect");
    });


    this.connection.on("ConnectionRejected", (payload: { reason?: string } | string | null) => {
      this.rejectedReason = typeof payload === "string" ? payload : payload?.reason ?? "connection_rejected";
      console.warn("[CoreSignalRClient] ConnectionRejected", { reason: this.rejectedReason });
      this.setState("disconnected");
      window.queueMicrotask(() => {
        void this.connection?.stop().catch(() => undefined);
      });
    });

    // Re-attach any handlers registered before connection existed
    for (const event of this.eventHandlers.keys()) this.attachServerEvent(event);

    try {
      await this.connection.start();
      if (this.rejectedReason) {
        throw new Error(`realtime_connection_rejected: ${this.rejectedReason}`);
      }
      this.setState("connected");
      await this.announceAvailable("connect");
    } catch (err) {

      this.setState("disconnected");
      if (this.rejectedReason) {
        throw new Error(`realtime_connection_rejected: ${this.rejectedReason}`);
      }
      throw err;
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.stop();
    } finally {
      this.connection = null;
      this.registered.clear();
      this.setState("disconnected");
    }
  }

  async reconnect(): Promise<void> {
    await this.disconnect();
    await this.connect({ tenantId: this.tenantId, agentId: this.agentId, token: this.token, url: this.url });
  }

  private attachServerEvent(event: ConceptualEvent) {
    if (!this.connection || this.registered.has(event)) return;
    const wire = SERVER_TO_CLIENT_EVENTS[event];
    if (!wire) return;
    this.connection.on(wire, (...args: any[]) => {
      const raw = args.length <= 1 ? args[0] : args;
      if (shouldLogRealtimeDebug()) {
        console.info("[CoreSignalRClient] event received", {
          event,
          wire,
          argCount: args.length,
          payloadKeys: raw && typeof raw === "object" && !Array.isArray(raw) ? Object.keys(raw).slice(0, 12) : [],
        });
      }
      const env = toEnvelope(event, this.tenantId, raw);
      this.eventHandlers.get(event)?.forEach((h) => h(env));
    });
    this.registered.add(event);
  }

  on<T = unknown>(
    event: ConceptualEvent,
    handler: (env: RealtimeEnvelope<T>) => void,
  ): Unsubscribe {
    let set = this.eventHandlers.get(event);
    if (!set) {
      set = new Set();
      this.eventHandlers.set(event, set);
    }
    set.add(handler as (env: RealtimeEnvelope) => void);
    this.attachServerEvent(event);
    return () => set!.delete(handler as (env: RealtimeEnvelope) => void);
  }

  off(event: ConceptualEvent, handler: (env: RealtimeEnvelope) => void): void {
    this.eventHandlers.get(event)?.delete(handler);
  }

  async invoke<R = void>(command: ConceptualCommand, payload: unknown): Promise<R> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      throw new Error(`realtime_not_connected (${command})`);
    }
    const wire = CLIENT_TO_SERVER_COMMANDS[command];
    if (!wire) throw new Error(`no_wire_mapping_for_${command}`);
    const adapter = COMMAND_ADAPTERS[command];
    const args = adapter ? adapter(payload) : [payload];
    return (await this.connection.invoke(wire, ...args)) as R;
  }

  async subscribeToAgent(): Promise<void> {
    /* server auto-adds agent group on connect */
  }
  async subscribeToTenant(): Promise<void> {
    /* not modelled on AgentHub */
  }
  async subscribeToRoom(): Promise<void> {
    /* server-managed via group membership */
  }
  async unsubscribeFromRoom(): Promise<void> {
    /* server-managed */
  }

  getConnectionState(): ConnectionState {
    return this.state;
  }

  onConnectionStateChanged(handler: (state: ConnectionState) => void): Unsubscribe {
    this.stateHandlers.add(handler);
    return () => this.stateHandlers.delete(handler);
  }
}
