/**
 * Resolves an inbound caller's display info from the SIP From: URI.
 *
 * Flow:
 *   1. Parse the From URI for a tenant extension (strips sip:, @host, AI-, -<tenantId>).
 *   2. If it matches a known user (useAgents → extension_number) → internal call, no API.
 *   3. If it's an AI pipeline (AI-<ext>) → label as bot, no API.
 *   4. Otherwise → POST /api/cti/caller-info/resolve to fetch CRM info.
 */
import { useQuery } from "@tanstack/react-query";
import { useAgents, agentLabel } from "@/hooks/useAgents";
import { parseSipIdentity } from "@/features/calls/sipIdentity";
import { getApiOrigin, getAccessToken } from "@/lib/apiClient";
import { getCurrentTenantId } from "@/lib/tenant";

export type InboundCallerKind = "internal" | "ai-pipeline" | "external" | "unknown";

export interface InboundCallerInfo {
  kind: InboundCallerKind;
  name?: string;
  extension?: string;
  isVip?: boolean;
  segment?: string;
  customerId?: string;
  loading?: boolean;
}

interface CtiResolveResponse {
  success?: boolean;
  callerInfo?: {
    phoneNumber?: string;
    name?: string;
    type?: string;
    isVip?: boolean;
    segment?: string;
    customerId?: string;
  } | null;
}

async function resolveCallerInfo(phoneNumber: string): Promise<CtiResolveResponse["callerInfo"]> {
  let tenantId: string | undefined;
  try { tenantId = getCurrentTenantId(); } catch { tenantId = undefined; }
  const token = getAccessToken();
  const res = await fetch(`${getApiOrigin()}/api/cti/caller-info/resolve`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ tenantId, phoneNumber }),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const json = (await res.json()) as CtiResolveResponse;
  return json?.callerInfo ?? null;
}

export function useInboundCaller(
  fromUri: string | undefined | null,
  number: string,
  direction?: "in" | "out",
): InboundCallerInfo {
  const agents = useAgents();
  const ident = parseSipIdentity(fromUri ?? number ?? "");
  const isExternal = ident.kind === "phone" || (!ident.extension && !!number);
  const isOutbound = direction === "out";

  // Always call useQuery (rules of hooks). Only enabled for inbound external numbers.
  const q = useQuery({
    queryKey: ["cti-caller", number],
    queryFn: () => resolveCallerInfo(number),
    enabled: !isOutbound && isExternal && !!number,
    staleTime: 60_000,
    retry: 0,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
  });

  if (ident.kind === "ext" && ident.extension) {
    const extNum = Number(ident.extension);
    const agent = agents.find((a) => a.extension_number === extNum);
    return {
      kind: "internal",
      extension: ident.extension,
      name: agent ? agentLabel(agent) : `Extension ${ident.extension}`,
    };
  }

  if (ident.kind === "ai" && ident.extension) {
    return {
      kind: "ai-pipeline",
      extension: ident.extension,
      name: `AI · ${ident.extension}`,
    };
  }

  if (q.isLoading) return { kind: "external", loading: true };
  const info = q.data;
  if (!info) return { kind: "unknown" };
  return {
    kind: "external",
    name: info.name && info.name !== "Unknown" ? info.name : undefined,
    isVip: !!info.isVip,
    segment: info.segment,
    customerId: info.customerId,
  };
}
