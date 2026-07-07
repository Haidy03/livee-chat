import { useFrontendTool } from "@/lib/copilotkit-compat";
import { z } from "zod";
import { useNavigate } from "react-router-dom";
import { api } from "@/lib/apiClient";
import { useAuth } from "@/hooks/useAuth";
import { computeDateRange } from "@/copilot/shared/dateRange";


interface Props {
  isRtl: boolean;
}

const dateParams = {
  timePeriod: z
    .enum(["today", "last7days", "last30days", "custom"])
    .optional()
    .describe("today | last7days | last30days | custom"),
  startDate: z.string().optional().describe("DD/MM/YYYY (required if custom)"),
  endDate: z.string().optional().describe("DD/MM/YYYY (required if custom)"),
};

const safeGet = async <T,>(path: string): Promise<T | null> => {
  try {
    return await api.get<T>(path);
  } catch {
    return null;
  }
};

/**
 * Registers all read-only data tools for the contact center.
 * Every handler returns { success: true, ... } or { success: false, error }.
 */
export function ContactCenterActions({ isRtl }: Props) {
  const { user } = useAuth();
  const navigate = useNavigate();

  const errAuth = isRtl ? "يجب تسجيل الدخول أولاً." : "Authentication required.";
  const errGeneric = (msg: string) =>
    isRtl ? `فشل التنفيذ: ${msg}` : `Failed: ${msg}`;
  const guard = () => (user ? null : { success: false as const, error: errAuth });

  // 1) Call statistics
  useFrontendTool({
    name: "getCallStatistics",
    description:
      "ALWAYS call when admin asks about call totals, missed counts, average wait or talk time over a period. Returns totals, missed, answered, avg wait/talk seconds.",
    parameters: z.object(dateParams),
    handler: async ({ timePeriod, startDate, endDate }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "today", startDate, endDate);
      try {
        const calls = await api.get<any[]>(
          `/calls?from=${encodeURIComponent(r.from.toISOString())}&to=${encodeURIComponent(r.to.toISOString())}`,
        );
        const list = Array.isArray(calls) ? calls : [];
        const missed = list.filter((c) => c?.status === "missed").length;
        const answered = list.filter((c) => c?.status === "answered" || c?.answeredAt).length;
        const totalRing = list.reduce((s, c) => s + (c?.ringSeconds ?? 0), 0);
        const totalTalk = list.reduce((s, c) => s + (c?.talkSeconds ?? 0), 0);
        return {
          success: true,
          period: { ...r, from: undefined, to: undefined },
          total: list.length,
          missed,
          answered,
          avgWaitSeconds: list.length ? Math.round(totalRing / list.length) : 0,
          avgTalkSeconds: list.length ? Math.round(totalTalk / list.length) : 0,
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "calls") };
      }
    },
  });

  // 2) Calls by agent
  useFrontendTool({
    name: "getCallsByAgent",
    description: "Top agents by call volume in a period.",
    parameters: z.object({ ...dateParams, limit: z.number().optional() }),
    handler: async ({ timePeriod, startDate, endDate, limit = 10 }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "today", startDate, endDate);
      try {
        const calls = (await api.get<any[]>(
          `/calls?from=${r.from.toISOString()}&to=${r.to.toISOString()}`,
        )) ?? [];
        const byAgent = new Map<string, { agent: string; total: number; missed: number }>();
        for (const c of calls) {
          const key = c?.agentName ?? c?.agentId ?? (isRtl ? "غير معروف" : "Unknown");
          const cur = byAgent.get(key) ?? { agent: key, total: 0, missed: 0 };
          cur.total += 1;
          if (c?.status === "missed") cur.missed += 1;
          byAgent.set(key, cur);
        }
        const top = [...byAgent.values()].sort((a, b) => b.total - a.total).slice(0, limit);
        return { success: true, period: { startDate: r.startDate, endDate: r.endDate }, agents: top };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "calls") };
      }
    },
  });

  // 3) Calls by queue
  useFrontendTool({
    name: "getCallsByQueue",
    description: "Per-queue inbound and abandoned counts in a period.",
    parameters: z.object(dateParams),
    handler: async ({ timePeriod, startDate, endDate }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "today", startDate, endDate);
      try {
        const calls = (await api.get<any[]>(
          `/calls?from=${r.from.toISOString()}&to=${r.to.toISOString()}`,
        )) ?? [];
        const byQ = new Map<string, { queue: string; total: number; abandoned: number }>();
        for (const c of calls) {
          const key = c?.queueName ?? c?.queueId ?? (isRtl ? "غير محدد" : "Unassigned");
          const cur = byQ.get(key) ?? { queue: key, total: 0, abandoned: 0 };
          cur.total += 1;
          if (c?.status === "missed" || c?.status === "abandoned") cur.abandoned += 1;
          byQ.set(key, cur);
        }
        return {
          success: true,
          period: { startDate: r.startDate, endDate: r.endDate },
          queues: [...byQ.values()].sort((a, b) => b.total - a.total),
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "calls") };
      }
    },
  });

  // 4) Calls by disposition
  useFrontendTool({
    name: "getCallsByDisposition",
    description: "Wrap-up code / disposition breakdown for a period.",
    parameters: z.object(dateParams),
    handler: async ({ timePeriod, startDate, endDate }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "today", startDate, endDate);
      try {
        const calls = (await api.get<any[]>(
          `/calls?from=${r.from.toISOString()}&to=${r.to.toISOString()}`,
        )) ?? [];
        const map = new Map<string, number>();
        for (const c of calls) {
          const k = c?.wrapUpCode ?? c?.disposition ?? (isRtl ? "بدون" : "None");
          map.set(k, (map.get(k) ?? 0) + 1);
        }
        return {
          success: true,
          period: { startDate: r.startDate, endDate: r.endDate },
          dispositions: [...map.entries()]
            .map(([code, count]) => ({ code, count }))
            .sort((a, b) => b.count - a.count),
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "calls") };
      }
    },
  });

  // 5) Missed calls (recent)
  useFrontendTool({
    name: "getMissedCalls",
    description: "Most recent missed calls. Useful when admin asks 'what missed calls do I have?'",
    parameters: z.object({ ...dateParams, limit: z.number().optional() }),
    handler: async ({ timePeriod, startDate, endDate, limit = 20 }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "today", startDate, endDate);
      try {
        const calls = (await api.get<any[]>(
          `/calls?from=${r.from.toISOString()}&to=${r.to.toISOString()}`,
        )) ?? [];
        const missed = calls
          .filter((c) => c?.status === "missed")
          .slice(0, limit)
          .map((c) => ({
            id: c.id,
            caller: c.caller,
            queue: c.queueName,
            at: c.startedAt,
            ringSeconds: c.ringSeconds,
          }));
        return {
          success: true,
          period: { startDate: r.startDate, endDate: r.endDate },
          count: missed.length,
          missed,
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "calls") };
      }
    },
  });

  // 6) Live queue snapshot
  useFrontendTool({
    name: "getLiveQueueSnapshot",
    description: "Current live snapshot: calls waiting, longest wait, active calls, agents online.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      const snap: any = await safeGet("/live/snapshot");
      if (!snap) return { success: false, error: isRtl ? "تعذر جلب اللقطة." : "Snapshot unavailable." };
      const agents = snap.agents ?? [];
      return {
        success: true,
        waiting: snap.queue?.waiting ?? 0,
        longestWaitSeconds: snap.queue?.longestWaitSeconds ?? 0,
        activeCalls: (snap.activeCalls ?? []).length,
        agents: {
          total: agents.length,
          available: agents.filter((a: any) => a.status === "available").length,
          busy: agents.filter((a: any) => a.status === "busy").length,
          break: agents.filter((a: any) => a.status === "break").length,
          offline: agents.filter((a: any) => a.status === "offline").length,
        },
      };
    },
  });

  // 7) Active agents
  useFrontendTool({
    name: "getActiveAgents",
    description: "Overall agent counts (online/offline) and the list of currently-online agents.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      try {
        const agents = (await api.get<any[]>("/agents")) ?? [];
        const online = agents.filter((a) => a.status && a.status !== "offline");
        return {
          success: true,
          total: agents.length,
          onlineCount: online.length,
          online: online.slice(0, 50).map((a) => ({
            id: a.id ?? a.userId,
            name: a.displayName ?? a.name,
            status: a.status,
            extension: a.extension,
          })),
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "agents") };
      }
    },
  });

  // 8) Single agent status
  useFrontendTool({
    name: "getAgentStatus",
    description: "Look up one agent's current live status by name (case-insensitive substring).",
    parameters: z.object({ agentName: z.string() }),
    handler: async ({ agentName }) => {
      const g = guard(); if (g) return g;
      try {
        const agents = (await api.get<any[]>("/agents")) ?? [];
        const needle = agentName.toLowerCase();
        const found = agents.find((a) =>
          (a.displayName ?? a.name ?? "").toLowerCase().includes(needle),
        );
        if (!found) return { success: false, error: isRtl ? "لم يتم العثور على الوكيل." : "Agent not found." };
        return { success: true, agent: { name: found.displayName ?? found.name, status: found.status, extension: found.extension } };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "agents") };
      }
    },
  });

  // 9) Campaign summary
  useFrontendTool({
    name: "getCampaignSummary",
    description: "Counts of campaigns by status (running, scheduled, completed, paused).",
    parameters: z.object({ status: z.string().optional() }),
    handler: async ({ status }) => {
      const g = guard(); if (g) return g;
      try {
        const list = (await api.get<any[]>("/campaigns")) ?? [];
        const filtered = status ? list.filter((c) => c.status === status) : list;
        const map: Record<string, number> = {};
        for (const c of list) {
          const s = c.status ?? "unknown";
          map[s] = (map[s] ?? 0) + 1;
        }
        return { success: true, total: list.length, byStatus: map, items: filtered.slice(0, 10).map((c) => ({ id: c.id, name: c.name, status: c.status })) };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "campaigns") };
      }
    },
  });

  // 10) Campaign targets progress
  useFrontendTool({
    name: "getCampaignTargetsProgress",
    description: "Pending/done/failed counts for a specific campaign's targets.",
    parameters: z.object({ campaignId: z.string() }),
    handler: async ({ campaignId }) => {
      const g = guard(); if (g) return g;
      try {
        const res: any = await api.get(`/campaigns/${encodeURIComponent(campaignId)}/targets/stats`);
        return { success: true, campaignId, ...res };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "campaign targets") };
      }
    },
  });

  // 11) Directory count
  useFrontendTool({
    name: "getDirectoryContactCount",
    description: "Total contacts in the directory, optionally matching a search query.",
    parameters: z.object({ query: z.string().optional() }),
    handler: async ({ query }) => {
      const g = guard(); if (g) return g;
      try {
        const path = query ? `/contacts?q=${encodeURIComponent(query)}` : "/contacts";
        const res: any = await api.get(path);
        const list = Array.isArray(res) ? res : res?.items ?? [];
        return { success: true, count: list.length, query: query ?? null };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "directory") };
      }
    },
  });

  // 12) Survey results
  useFrontendTool({
    name: "getSurveyResults",
    description: "Survey response counts and average score. Provide surveyId for a single survey or a time period for all.",
    parameters: z.object({ surveyId: z.string().optional(), ...dateParams }),
    handler: async ({ surveyId, timePeriod, startDate, endDate }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "last30days", startDate, endDate);
      try {
        const path = surveyId ? `/surveys/${encodeURIComponent(surveyId)}/results` : "/surveys/results";
        const res: any = await api.get(path);
        const responses = res?.responses ?? res?.items ?? [];
        const scores = responses.map((x: any) => x?.score).filter((n: any) => typeof n === "number");
        const avg = scores.length ? scores.reduce((s: number, n: number) => s + n, 0) / scores.length : null;
        return { success: true, period: { startDate: r.startDate, endDate: r.endDate }, surveyId: surveyId ?? null, count: responses.length, avgScore: avg };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "surveys") };
      }
    },
  });

  // 13) Recent recordings
  useFrontendTool({
    name: "getRecentRecordings",
    description: "Recent calls that have recording URLs.",
    parameters: z.object({ limit: z.number().optional() }),
    handler: async ({ limit = 10 }) => {
      const g = guard(); if (g) return g;
      try {
        const calls = (await api.get<any[]>("/calls?hasRecording=true")) ?? [];
        return {
          success: true,
          recordings: calls.slice(0, limit).map((c) => ({
            id: c.id, caller: c.caller, agent: c.agentName, at: c.startedAt, recordingUrl: c.recordingUrl,
          })),
        };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "recordings") };
      }
    },
  });

  // 14) Billing balance
  useFrontendTool({
    name: "getBillingBalance",
    description: "Current account balance: uninvoiced amount, available balance, currency.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      try {
        const res: any = await api.get("/billing/balance");
        return { success: true, ...res };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "billing") };
      }
    },
  });

  // 15) Billing summary
  useFrontendTool({
    name: "getBillingSummary",
    description: "General billing info: company name, billing address, VAT number, currency.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      try {
        const res: any = await api.get("/billing");
        return { success: true, ...res };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "billing") };
      }
    },
  });

  // 16) Auto-tags
  useFrontendTool({
    name: "getAutoTagsList",
    description: "Configured auto-tags catalog.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      try {
        const list = (await api.get<any[]>("/auto-tags")) ?? [];
        return { success: true, count: list.length, tags: list.map((t) => ({ id: t.id, name: t.name, color: t.color })) };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "auto-tags") };
      }
    },
  });

  // 17) Skills + wrap-up codes
  useFrontendTool({
    name: "getSkillsAndWrapUpCodes",
    description: "Summary of configured skills and wrap-up codes.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      const [skills, wrap] = await Promise.all([
        safeGet<any[]>("/skills"),
        safeGet<any[]>("/wrap-up-codes"),
      ]);
      return {
        success: true,
        skills: { count: skills?.length ?? 0, items: (skills ?? []).slice(0, 50).map((s: any) => s.name) },
        wrapUpCodes: { count: wrap?.length ?? 0, items: (wrap ?? []).slice(0, 50).map((c: any) => c.name) },
      };
    },
  });

  // 18) Edit logs
  useFrontendTool({
    name: "getEditLogs",
    description: "Audit / edit-log entries within a period, optionally filtered by module.",
    parameters: z.object({ ...dateParams, module: z.string().optional() }),
    handler: async ({ timePeriod, startDate, endDate, module }) => {
      const g = guard(); if (g) return g;
      const r = computeDateRange(timePeriod ?? "last7days", startDate, endDate);
      try {
        const qs = new URLSearchParams({ from: r.from.toISOString(), to: r.to.toISOString() });
        if (module) qs.set("module", module);
        const res: any = await api.get(`/edit-logs?${qs.toString()}`);
        const items = Array.isArray(res) ? res : res?.items ?? [];
        return { success: true, period: { startDate: r.startDate, endDate: r.endDate }, count: items.length, items: items.slice(0, 20) };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "edit-logs") };
      }
    },
  });

  // 19) Users + groups
  useFrontendTool({
    name: "getUserAndGroupCount",
    description: "Total users and total groups in this tenant.",
    parameters: z.object({}),
    handler: async () => {
      const g = guard(); if (g) return g;
      const [users, groups] = await Promise.all([
        safeGet<any[]>("/users"),
        safeGet<any[]>("/groups"),
      ]);
      return {
        success: true,
        users: { count: users?.length ?? 0 },
        groups: { count: groups?.length ?? 0 },
      };
    },
  });

  // 20) Role assignments
  useFrontendTool({
    name: "getRoleAssignments",
    description: "Read-only view of RBAC role assignments. Optionally filter by userId.",
    parameters: z.object({ userId: z.string().optional() }),
    handler: async ({ userId }) => {
      const g = guard(); if (g) return g;
      try {
        const path = userId ? `/rbac/user-roles?userId=${encodeURIComponent(userId)}` : "/rbac/user-roles";
        const list = (await api.get<any[]>(path)) ?? [];
        return { success: true, count: list.length, assignments: list };
      } catch (e: any) {
        return { success: false, error: errGeneric(e?.message ?? "rbac") };
      }
    },
  });


  return null;
}

