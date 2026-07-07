import { useEffect, useState } from "react";
import { api } from "@/lib/apiClient";

export interface AgentProfile {
  user_id: string;
  first_name: string | null;
  last_name: string | null;
  email: string | null;
  display_name: string | null;
  role: string;
  extension_number: number | null;
  groups: string[];
}

export function agentLabel(a: AgentProfile | undefined | null): string {
  if (!a) return "—";
  const full = [a.first_name, a.last_name].filter(Boolean).join(" ").trim();
  return full || a.display_name || a.email || "—";
}

export function agentInitials(a: AgentProfile | undefined | null): string {
  const label = agentLabel(a);
  if (!label || label === "—") return "—";
  const parts = label.split(/\s+/).filter(Boolean);
  return parts.slice(0, 2).map((p) => p[0]?.toUpperCase() ?? "").join("") || "—";
}

const AVATAR_COLORS = [
  "bg-emerald-500", "bg-violet-500", "bg-blue-500", "bg-amber-500",
  "bg-rose-500", "bg-cyan-500", "bg-indigo-500", "bg-pink-500",
];

export function agentAvatarBg(id: string | null | undefined): string {
  if (!id) return "bg-muted";
  let h = 0;
  for (let i = 0; i < id.length; i++) h = (h * 31 + id.charCodeAt(i)) >>> 0;
  return AVATAR_COLORS[h % AVATAR_COLORS.length];
}

let cache: AgentProfile[] | null = null;
const listeners = new Set<(a: AgentProfile[]) => void>();

async function fetchAgents() {
  const rows = await api.get<
    {
      userId: string;
      firstName?: string | null;
      lastName?: string | null;
      email?: string | null;
      displayName: string;
      role: string;
      extensionNumber?: number | null;
      groups?: string[] | null;
    }[]
  >("/profiles");
  cache = (rows ?? []).map((p) => ({
    user_id: p.userId,
    first_name: p.firstName ?? null,
    last_name: p.lastName ?? null,
    email: p.email ?? null,
    display_name: p.displayName ?? null,
    role: p.role,
    extension_number: p.extensionNumber ?? null,
    groups: Array.isArray(p.groups) ? p.groups : [],
  }));
  listeners.forEach((fn) => fn(cache!));
  return cache;
}

export function getAgentsCache(): AgentProfile[] {
  return cache ?? [];
}

export function useAgents() {
  const [agents, setAgents] = useState<AgentProfile[]>(cache ?? []);
  useEffect(() => {
    listeners.add(setAgents);
    if (!cache) fetchAgents();
    else setAgents(cache);
    return () => { listeners.delete(setAgents); };
  }, []);
  return agents;
}

export function useAgentById(id: string | null | undefined) {
  const agents = useAgents();
  if (!id) return undefined;
  return agents.find((a) => a.user_id === id);
}
