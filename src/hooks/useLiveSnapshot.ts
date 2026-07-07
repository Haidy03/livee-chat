import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";

export type AgentLiveStatus = "available" | "busy" | "break" | "offline";
export type ActiveCallStatus = "ringing" | "answered" | "hold";

export interface ActiveCall {
  id: string;
  caller: string;
  called: string;
  agentId?: string | null;
  groupId?: string | null;
  startedAt: string;
  answeredAt?: string | null;
  status: ActiveCallStatus;
  /** Full Asterisk channel name (e.g. `PJSIP/1001-xxx-0000001a`). */
  asteriskChannel?: string | null;
  /** Asterisk channel uniqueid (e.g. `1780322607.42`). Preferred ChanSpy target. */
  asteriskUniqueId?: string | null;
}

export interface QueueStats {
  waiting: number;
  longestWaitSeconds: number;
}

export interface LiveAgent {
  userId: string;
  displayName: string;
  extension?: number | null;
  status: AgentLiveStatus;
  currentCallId?: string | null;
  lastChangeAt: string;
}

export interface LiveSnapshot {
  activeCalls: ActiveCall[];
  queue: QueueStats;
  agents: LiveAgent[];
}

const EMPTY: LiveSnapshot = {
  activeCalls: [],
  queue: { waiting: 0, longestWaitSeconds: 0 },
  agents: [],
};

const VALID_STATUSES: AgentLiveStatus[] = ["available", "busy", "break", "offline"];
function normalizeStatus(s: unknown): AgentLiveStatus {
  if (s === "online") return "available";
  return VALID_STATUSES.includes(s as AgentLiveStatus) ? (s as AgentLiveStatus) : "offline";
}

export function useLiveSnapshot(
  options: { enabled?: boolean; refetchInterval?: number | false } = {}
) {
  const { enabled = true, refetchInterval = 3000 } = options;
  return useQuery({
    queryKey: ["live-snapshot"],
    queryFn: async (): Promise<LiveSnapshot> => {
      const data = await api.get<LiveSnapshot>("/live/snapshot");
      return {
        activeCalls: data?.activeCalls ?? [],
        queue: data?.queue ?? EMPTY.queue,
        agents: (data?.agents ?? []).map((a) => ({ ...a, status: normalizeStatus(a.status) })),
      };
    },
    enabled,
    refetchInterval,
    refetchOnWindowFocus: true,
    placeholderData: EMPTY,
  });
}
