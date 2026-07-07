import type { IRealtimeClient } from "./IRealtimeClient";
import { getMockClient } from "./MockRealtimeClient";
import { LegacySignalRClient } from "./LegacySignalRClient";
import { CoreSignalRClient } from "./CoreSignalRClient";

export type RealtimeMode = "mock" | "legacy" | "core";

export function getRealtimeMode(): RealtimeMode {
  const fromMode = (import.meta.env.VITE_REALTIME_MODE as string | undefined)?.toLowerCase();
  if (fromMode === "mock" || fromMode === "legacy" || fromMode === "core") return fromMode;
  const impl = (import.meta.env.VITE_SIGNALR_IMPLEMENTATION as string | undefined)?.toLowerCase();
  if (impl === "legacy") return "legacy";
  if (impl === "core") return "core";
  return "mock";
}

let _client: IRealtimeClient | null = null;
export function createRealtimeClient(): IRealtimeClient {
  if (_client) return _client;
  const mode = getRealtimeMode();
  switch (mode) {
    case "legacy":
      _client = new LegacySignalRClient();
      break;
    case "core":
      _client = new CoreSignalRClient();
      break;
    default:
      _client = getMockClient();
  }
  return _client;
}

export function isMockMode() {
  return getRealtimeMode() === "mock";
}
