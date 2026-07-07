/**
 * Resolves a SIP URI to a tenant user or AI pipeline for enriching
 * /calls/sip-upsert payloads.
 */

import { api } from "@/lib/apiClient";
import { getCurrentTenantId } from "@/lib/tenant";
import { parseSipIdentity } from "@/features/calls/sipIdentity";
import { getAgentsCache, agentLabel, type AgentProfile } from "@/hooks/useAgents";

export interface ResolvedParty {
  id?: string;
  name?: string;
  extension?: string;
  isAiAgent: boolean;
}

interface PipelineEntry {
  id: string;
  name: string;
  extension: string;
}

let pipelinesCache: PipelineEntry[] | null = null;
let pipelinesFetchedAt = 0;
let pipelinesInflight: Promise<PipelineEntry[]> | null = null;
const TTL_MS = 60_000;

async function getPipelines(): Promise<PipelineEntry[]> {
  const now = Date.now();
  if (pipelinesCache && now - pipelinesFetchedAt < TTL_MS) return pipelinesCache;
  if (pipelinesInflight) return pipelinesInflight;

  let tid: string | null = null;
  try { tid = getCurrentTenantId(); } catch { tid = null; }
  if (!tid) return [];

  pipelinesInflight = (async () => {
    try {
      const res = await api.get<any>(`/voicebot/projects/${tid}/pipelines`);
      const rows: any[] = Array.isArray(res) ? res : (res?.items ?? []);
      pipelinesCache = rows
        .filter((r) => r?.active === true && r?.extension)
        .map((r) => ({ id: String(r.id), name: r.name ?? "Pipeline", extension: String(r.extension) }));
      pipelinesFetchedAt = Date.now();
      return pipelinesCache;
    } catch {
      return pipelinesCache ?? [];
    } finally {
      pipelinesInflight = null;
    }
  })();
  return pipelinesInflight;
}

let agentsLocalCache: AgentProfile[] | null = null;
let agentsFetchedAt = 0;
let agentsInflight: Promise<AgentProfile[]> | null = null;

async function getAgents(): Promise<AgentProfile[]> {
  const fromHook = getAgentsCache();
  if (fromHook.length > 0) return fromHook;
  const now = Date.now();
  if (agentsLocalCache && now - agentsFetchedAt < TTL_MS) return agentsLocalCache;
  if (agentsInflight) return agentsInflight;

  agentsInflight = (async () => {
    try {
      const rows = await api.get<any[]>("/profiles").catch(() => [] as any[]);
      agentsLocalCache = (rows ?? []).map((p: any) => ({
        user_id: p.userId,
        first_name: p.firstName ?? null,
        last_name: p.lastName ?? null,
        email: p.email ?? null,
        display_name: p.displayName ?? null,
        role: p.role,
        extension_number: p.extensionNumber ?? null,
        groups: Array.isArray(p.groups) ? p.groups : [],
      }));
      agentsFetchedAt = Date.now();
      return agentsLocalCache;
    } finally {
      agentsInflight = null;
    }
  })();
  return agentsInflight;
}

export async function resolveSipParty(uri: string | undefined | null): Promise<ResolvedParty | null> {
  if (!uri) return null;
  const ident = parseSipIdentity(uri);
  if (ident.kind === "phone" || !ident.extension) return null;

  if (ident.kind === "ext") {
    const extNum = Number(ident.extension);
    if (!Number.isInteger(extNum)) return null;
    const agents = await getAgents();
    const a = agents.find((x) => x.extension_number === extNum);
    if (!a) return null;
    return {
      id: a.user_id,
      name: agentLabel(a),
      extension: ident.extension,
      isAiAgent: false,
    };
  }

  if (ident.kind === "ai") {
    const list = await getPipelines();
    const p = list.find((x) => x.extension === ident.extension);
    return {
      id: p?.id,
      name: p?.name,
      extension: ident.extension,
      isAiAgent: true,
    };
  }

  return null;
}
