import type { ConnectionState, RealtimeEnvelope } from "@/features/digital-workspace/models";
import type {
  ConceptualCommand,
  ConceptualEvent,
  ConnectOptions,
  IRealtimeClient,
  Unsubscribe,
} from "./IRealtimeClient";

/**
 * Stub for jquery.signalR (ASP.NET legacy). Implement against /agentsHub when wiring.
 * See docs/digital/SIGNALR_INTEGRATION.md.
 */
export class LegacySignalRClient implements IRealtimeClient {
  private notImpl(): never {
    throw new Error(
      "LegacySignalRClient is not yet implemented. Set VITE_REALTIME_MODE=mock during frontend development.",
    );
  }
  async connect(_o: ConnectOptions) { this.notImpl(); }
  async disconnect() { this.notImpl(); }
  async reconnect() { this.notImpl(); }
  on<T>(_e: ConceptualEvent, _h: (env: RealtimeEnvelope<T>) => void): Unsubscribe { return this.notImpl(); }
  off() { this.notImpl(); }
  async invoke<R>(_c: ConceptualCommand, _p: unknown): Promise<R> { return this.notImpl(); }
  async subscribeToAgent() { this.notImpl(); }
  async subscribeToTenant() { this.notImpl(); }
  async subscribeToRoom() { this.notImpl(); }
  async unsubscribeFromRoom() { this.notImpl(); }
  getConnectionState(): ConnectionState { return "disconnected"; }
  onConnectionStateChanged(): Unsubscribe { return () => {}; }
}
