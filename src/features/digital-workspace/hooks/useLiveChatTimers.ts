import { useEffect, useRef } from "react";
import {
  useAgentConfigStore,
  useLiveChatTimerStore,
  useMessageStore,
  useRoomStore,
  useSessionStore,
  type LiveChatTimerSpan,
} from "../stores";
import { getProjectUser } from "../api/projectUser";
import type { Room } from "../models";

// ============= Placeholders — TODO(livechat-timers): wire to real APIs/SignalR =============
function setInActive(value: boolean) {
  // eslint-disable-next-line no-console
  console.log("[livechat] setInActive:", value);
}
function setAgentActive() {
  // eslint-disable-next-line no-console
  console.log("[livechat] setAgentActive heartbeat");
}
function notifyClientBeforClose(userId: string) {
  // eslint-disable-next-line no-console
  console.log("[livechat] notifyClientBeforClose:", userId);
}
function closeConversation(roomId: string, reasonCode: number) {
  // eslint-disable-next-line no-console
  console.log("[livechat] closeConversation:", roomId, "reason:", reasonCode);
}
function releaseClientRoom(roomId: string) {
  // eslint-disable-next-line no-console
  console.log("[livechat] releaseClientRoom:", roomId);
}
// ==========================================================================================

interface RoomTimerState {
  lastAgentMessageTime: number | null;
  lastUtteranceTime: number | null;
  notifyTime: number | null;
  offlineSince: number | null;
  deleted: boolean;
}

const ACTIVE_STATES = new Set(["waiting_customer", "pending", "escalated", "on_hold", "assigned"]);

function isActiveRoom(r: Room, meId: string): boolean {
  if (r.assignedAgentId !== meId) return false;
  if (["resolved", "closed", "spam"].includes(r.status)) return false;
  return ACTIVE_STATES.has(r.status) || true; // treat all non-terminal, mine as active
}

/**
 * Hydrates LiveChat agent config from GET /Account/GetProjectUser and runs the
 * 1-second inactivity/offline timer loop translated from the legacy AngularJS
 * `updateClientTime` function. Mount once at the /digital/livechat page level.
 */
export function useLiveChatTimers() {
  const setFromApi = useAgentConfigStore((s) => s.setFromApi);
  const setSpans = useLiveChatTimerStore((s) => s.setSpans);
  const meId = useSessionStore((s) => s.agentId || "agent-me");

  // Ephemeral per-room state; not in a store to avoid re-render storms.
  const roomTimersRef = useRef<Map<string, RoomTimerState>>(new Map());
  const prevSpansRef = useRef<Record<string, LiveChatTimerSpan>>({});
  const agentRef = useRef<{
    lastActivityTime: number | null;
    lastActiveTime: number;
    online: boolean;
  }>({
    lastActivityTime: null,
    lastActiveTime: Date.now(),
    online: true,
  });

  // 1) Fetch config once on mount.
  useEffect(() => {
    let cancelled = false;
    getProjectUser()
      .then((cfg) => {
        if (cancelled) return;
        const cur = useAgentConfigStore.getState();
        if (!cur.loaded) setFromApi(cfg);
      })
      .catch((err) => {
        // eslint-disable-next-line no-console
        console.warn("[livechat] failed to load project/user config", err);
      });
    return () => {
      cancelled = true;
    };
  }, [setFromApi]);

  // 2) Timer loop.
  useEffect(() => {
    const tick = () => {
      const cfg = useAgentConfigStore.getState();
      if (!cfg.loaded) return;

      const now = Date.now();
      const rooms = useRoomStore.getState();
      const msgs = useMessageStore.getState();

      const activeRooms = rooms.order
        .map((id) => rooms.byId[id])
        .filter((r): r is Room => Boolean(r) && isActiveRoom(r, meId));

      // ---- Agent-level inactivity (AngularJS parity) ----
      const agent = agentRef.current;
      const clientsCount = activeRooms.length;

      if (agent.lastActivityTime != null && clientsCount === 0) {
        const activeSpan = Math.floor((now - agent.lastActivityTime) / 1000);
        if (activeSpan > cfg.maxAgentInActiveWithClient) {
          setInActive(true);
        }
        agent.lastActivityTime = null;
      }
      if (agent.lastActivityTime == null && clientsCount > 0) {
        agent.lastActivityTime = now;
      }
      if (agent.lastActivityTime != null) {
        const activeSpan = Math.floor((now - agent.lastActivityTime) / 1000);
        if (activeSpan >= cfg.maxAgentInActive) {
          setInActive(true);
        }
      }
      if (agent.online) {
        const activeSpan = Math.floor((now - agent.lastActiveTime) / 1000);
        if (activeSpan >= 30) {
          setAgentActive();
          agent.lastActiveTime = now;
        }
      }

      // ---- Per-room ----
      const nextSpans: Record<string, LiveChatTimerSpan> = {};
      const seen = new Set<string>();

      for (const room of activeRooms) {
        seen.add(room.id);
        let state = roomTimersRef.current.get(room.id);
        if (!state) {
          state = {
            lastAgentMessageTime: null,
            lastUtteranceTime: null,
            notifyTime: null,
            offlineSince: null,
            deleted: false,
          };
          roomTimersRef.current.set(room.id, state);
        }

        // Derive last agent / last customer message from message store.
        const ids = msgs.byRoom[room.id] ?? [];
        let lastAgentAt: number | null = null;
        let lastCustomerAt: number | null = null;
        for (let i = ids.length - 1; i >= 0; i--) {
          const m = msgs.byId[ids[i]];
          if (!m) continue;
          const t = +new Date(m.sentAt);
          if (!lastAgentAt && (m.senderType === "agent" || m.senderType === "supervisor" || m.senderType === "bot")) {
            lastAgentAt = t;
          } else if (!lastCustomerAt && m.senderType === "customer") {
            lastCustomerAt = t;
          }
          if (lastAgentAt && lastCustomerAt) break;
        }

        // Client "utterance" is unanswered when customer spoke AFTER agent (or agent never replied).
        const utteranceUnanswered =
          lastCustomerAt != null && (lastAgentAt == null || lastCustomerAt > lastAgentAt);
        // Client is "silent after agent message" when agent spoke last.
        const clientSilent = lastAgentAt != null && (lastCustomerAt == null || lastAgentAt > lastCustomerAt);

        state.lastUtteranceTime = utteranceUnanswered ? lastCustomerAt : null;
        state.lastAgentMessageTime = clientSilent ? lastAgentAt : null;

        // Offline detection — use room status. Real presence would come via SignalR.
        const isOffline = room.status === "on_hold" || room.customerTyping === false && false;
        // We don't have a first-class "customer offline" signal in the model, so we
        // key off room.status === "snoozed" as a proxy that a customer disconnected.
        const offline = room.status === "snoozed";
        if (offline) {
          if (state.offlineSince == null) state.offlineSince = now;
        } else {
          state.offlineSince = null;
        }

        let agentMessageSpan = 0;
        let utteranceSpan = 0;
        let offlineSpan = 0;

        // Offline handling first — mirrors handelOffLineClient.
        if (offline && state.offlineSince != null) {
          offlineSpan = Math.floor((now - state.offlineSince) / 1000);
          if (offlineSpan >= cfg.clientDisconnectedTimeout && !state.deleted) {
            state.deleted = true;
            closeConversation(room.id, 8); // 8 = client disconnected
          }
        }

        // Agent inactivity after client message → releaseClientRoom
        if (state.lastUtteranceTime != null) {
          utteranceSpan = Math.floor((now - state.lastUtteranceTime) / 1000) + 1;
          if (utteranceSpan > cfg.maxAgentInActiveWithClient && !state.deleted) {
            releaseClientRoom(room.id);
          }
        }

        // Client inactivity after agent message → notify then close(7)
        if (state.lastAgentMessageTime != null) {
          agentMessageSpan = Math.floor((now - state.lastAgentMessageTime) / 1000) + 1;
          if (agentMessageSpan >= cfg.maxInActiveClient && !state.deleted) {
            if (state.notifyTime != null) {
              const sinceNotify = Math.floor((now - state.notifyTime) / 1000);
              if (sinceNotify > 30) {
                state.deleted = true;
                closeConversation(room.id, 7); // 7 = client not responding
              }
            } else {
              state.notifyTime = now;
              notifyClientBeforClose(room.id);
            }
          }
        } else {
          state.notifyTime = null;
        }

        nextSpans[room.id] = {
          agentMessageSpan,
          utteranceSpan,
          offlineSpan,
          notifyPending: state.notifyTime != null,
          deleted: state.deleted,
        };
      }

      // Cleanup stale room timer state.
      for (const key of Array.from(roomTimersRef.current.keys())) {
        if (!seen.has(key)) roomTimersRef.current.delete(key);
      }

      // Shallow-diff against last published spans; only write when something changed.
      const prev = prevSpansRef.current;
      const prevKeys = Object.keys(prev);
      const nextKeys = Object.keys(nextSpans);
      let changed = prevKeys.length !== nextKeys.length;
      if (!changed) {
        for (const k of nextKeys) {
          const a = prev[k];
          const b = nextSpans[k];
          if (
            !a ||
            a.agentMessageSpan !== b.agentMessageSpan ||
            a.utteranceSpan !== b.utteranceSpan ||
            a.offlineSpan !== b.offlineSpan ||
            a.notifyPending !== b.notifyPending ||
            a.deleted !== b.deleted
          ) {
            changed = true;
            break;
          }
        }
      }
      if (changed) {
        prevSpansRef.current = nextSpans;
        setSpans(nextSpans);
      }
    };

    const id = window.setInterval(tick, 1000);
    return () => window.clearInterval(id);
  }, [meId, setSpans]);
}
