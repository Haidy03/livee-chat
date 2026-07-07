import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";
import type { WrapUpCode, WrapUpCodeInput } from "./types";

const KEY = ["wrapup_codes"] as const;
const QKEY = (queueId: string) => ["queue_wrapup_codes", queueId] as const;
const QEFFKEY = (queueId: string) => ["queue_wrapup_codes_effective", queueId] as const;

interface WrapUpCodeDto {
  id: string;
  tenantId: string;
  label: string;
  labelAr?: string | null;
  category: string;
  color: string;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

function rowToCode(r: WrapUpCodeDto): WrapUpCode {
  return {
    id: r.id,
    tenantId: r.tenantId,
    label: r.label,
    labelAr: r.labelAr ?? null,
    category: r.category,
    color: r.color,
    isActive: r.isActive,
    sortOrder: r.sortOrder,
    createdAt: r.createdAt,
    updatedAt: r.updatedAt,
  };
}

function buildBody(input: Partial<WrapUpCodeInput>) {
  const out: Record<string, unknown> = {};
  if (input.label !== undefined) out.label = input.label;
  if (input.labelAr !== undefined) out.labelAr = input.labelAr;
  if (input.category !== undefined) out.category = input.category;
  if (input.color !== undefined) out.color = input.color;
  if (input.isActive !== undefined) out.isActive = input.isActive;
  if (input.sortOrder !== undefined) out.sortOrder = input.sortOrder;
  return out;
}

export function useWrapUpCodes(opts: { activeOnly?: boolean } = {}) {
  const activeOnly = opts.activeOnly ?? false;
  return useQuery({
    queryKey: [...KEY, activeOnly],
    queryFn: async (): Promise<WrapUpCode[]> => {
      const data = await api.get<WrapUpCodeDto[]>(
        `/wrap-up-codes?activeOnly=${activeOnly ? "true" : "false"}`,
      );
      return (data ?? []).map(rowToCode);
    },
  });
}

export function useCreateWrapUpCode() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: WrapUpCodeInput) => {
      const data = await api.post<WrapUpCodeDto>("/wrap-up-codes", {
        label: input.label,
        labelAr: input.labelAr ?? null,
        category: input.category ?? "general",
        color: input.color ?? "#64748b",
        isActive: input.isActive ?? true,
        sortOrder: input.sortOrder ?? 0,
      });
      return rowToCode(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateWrapUpCode() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, patch }: { id: string; patch: Partial<WrapUpCodeInput> }) => {
      const data = await api.put<WrapUpCodeDto>(`/wrap-up-codes/${id}`, buildBody(patch));
      return rowToCode(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteWrapUpCode() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.deleteRaw(`/wrap-up-codes/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

// ---- Queue ↔ wrap-up code mapping ----

export function useQueueWrapUpCodeIds(queueId: string | undefined) {
  return useQuery({
    queryKey: QKEY(queueId ?? ""),
    enabled: !!queueId,
    queryFn: async (): Promise<string[]> => {
      const data = await api.get<string[]>(`/wrap-up-codes/queues/${queueId}`);
      return data ?? [];
    },
  });
}

export function useSetQueueWrapUpCodes() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ queueId, codeIds }: { queueId: string; codeIds: string[] }) => {
      await api.put(`/wrap-up-codes/queues/${queueId}`, { codeIds });
    },
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: QKEY(vars.queueId) });
      qc.invalidateQueries({ queryKey: QEFFKEY(vars.queueId) });
    },
  });
}

// Resolve codes to use for a specific queue. Falls back to all active tenant
// codes when no queueId is provided.
export function useEffectiveWrapUpCodesForQueue(queueId: string | undefined) {
  const fallback = useWrapUpCodes({ activeOnly: true });
  const effectiveQuery = useQuery({
    queryKey: QEFFKEY(queueId ?? ""),
    enabled: !!queueId,
    queryFn: async (): Promise<WrapUpCode[]> => {
      const data = await api.get<WrapUpCodeDto[]>(
        `/wrap-up-codes/queues/${queueId}/effective`,
      );
      return (data ?? []).map(rowToCode);
    },
  });

  if (!queueId) {
    return { codes: fallback.data ?? [], isLoading: fallback.isLoading };
  }
  return {
    codes: effectiveQuery.data ?? [],
    isLoading: effectiveQuery.isLoading,
  };
}
