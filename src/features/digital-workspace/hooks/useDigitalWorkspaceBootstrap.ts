import { useEffect, useRef } from "react";
import {
  useAgentStore,
  useRoomStore,
  useCustomerStore,
  useKnowledgeStore,
  useMessageStore,
  useQueueStore,
} from "../stores";
import {
  MOCK_AGENTS,
  MOCK_CASES,
  MOCK_ROOMS,
  MOCK_CUSTOMERS,
  MOCK_JOURNEY,
  MOCK_KNOWLEDGE,
  MOCK_MESSAGES,
  MOCK_QUEUES,
  TENANT_ID,
} from "../mocks";
import { createRealtimeClient, getRealtimeMode, isMockMode } from "@/infrastructure/realtime/RealtimeClientFactory";
import { wireDispatcher } from "@/infrastructure/realtime/RealtimeEventDispatcher";
import { useRealtimeStore, useSessionStore } from "../stores";

// Module-level guard so StrictMode double-invoke and remounts don't tear
// down and reconnect the shared realtime client on every mount cycle.
let refCount = 0;
let seeded = false;
let sharedOff: (() => void) | null = null;
let sharedDisconnect: (() => void) | null = null;

/** Seeds mock data once and wires up the realtime dispatcher. */
export function useDigitalWorkspaceBootstrap() {
  const mountedRef = useRef(false);

  useEffect(() => {
    if (mountedRef.current) return;
    mountedRef.current = true;
    refCount += 1;

    const mode = getRealtimeMode();
    const seedMocks = mode !== "core";

    if (!seeded) {
      seeded = true;
      useAgentStore.getState().setAgents(MOCK_AGENTS);
      useQueueStore.getState().setQueues(MOCK_QUEUES);
      useCustomerStore.getState().setCustomers(MOCK_CUSTOMERS);
      Object.entries(MOCK_JOURNEY).forEach(([cid, items]) =>
        useCustomerStore.getState().setJourney(cid, items),
      );
      Object.entries(MOCK_CASES).forEach(([cid, items]) =>
        useCustomerStore.getState().setCases(cid, items),
      );
      useKnowledgeStore.getState().setArticles(MOCK_KNOWLEDGE);

      if (seedMocks) {
        useRoomStore.getState().setRooms(MOCK_ROOMS);
        Object.entries(MOCK_MESSAGES).forEach(([cid, list]) =>
          useMessageStore.getState().setMessages(cid, list),
        );
        const me = "agent-me";
        const firstMine = MOCK_ROOMS.find((c) => c.assignedAgentId === me);
        if (firstMine) useRoomStore.getState().openTab(firstMine.id);
      }
    }

    // Wire realtime only once for the shared singleton client.
    if (!sharedOff) {
      const client = createRealtimeClient();
      sharedOff = wireDispatcher(client);

      const session = useSessionStore.getState();
      const tenantId = mode === "core" ? session.tenantId : TENANT_ID;
      const agentId = session.agentId || "agent-me";

      client
        .connect({ tenantId, agentId })
        .catch((err) => {
          console.warn("[DigitalWorkspace] realtime connect failed", err);
          useRealtimeStore.getState().setConnectionState("disconnected");
        });
      if (isMockMode()) {
        console.info("[DigitalWorkspace] running in mock realtime mode");
      }

      sharedDisconnect = () => {
        client.disconnect().catch(() => {});
      };
    }

    return () => {
      mountedRef.current = false;
      refCount = Math.max(0, refCount - 1);
      if (refCount === 0) {
        sharedOff?.();
        sharedOff = null;
        sharedDisconnect?.();
        sharedDisconnect = null;
      }
    };
  }, []);
}
