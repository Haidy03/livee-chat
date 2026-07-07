import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";
import type { Queue } from "./types";

export type QueueWritePayload = Omit<Queue, "id" | "createdAt" | "updatedAt">;

const QUEUES_KEY = ["queues"] as const;

let queuesCache: Queue[] = [];
export function getQueuesCache(): Queue[] {
  return queuesCache;
}

export function useQueues() {
  const q = useQuery({
    queryKey: QUEUES_KEY,
    queryFn: async (): Promise<Queue[]> => {
      const rows = await api.get<Queue[]>("/queues");
      return rows ?? [];
    },
  });
  if (q.data && q.data !== queuesCache) queuesCache = q.data;
  return q;
}

export function useQueue(id: string | undefined) {
  return useQuery({
    queryKey: [...QUEUES_KEY, id],
    enabled: !!id,
    queryFn: async (): Promise<Queue> => api.get<Queue>(`/queues/${id}`),
  });
}

function toPayload(q: Queue | QueueWritePayload): QueueWritePayload {
  const { id: _id, createdAt: _c, updatedAt: _u, ...rest } = q as Queue;
  return rest;
}

export function useCreateQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: Queue | QueueWritePayload) =>
      api.post<Queue>("/queues", toPayload(input)),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUEUES_KEY }),
  });
}

export function useUpdateQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, queue }: { id: string; queue: Queue | QueueWritePayload }) =>
      api.patch<Queue>(`/queues/${id}`, toPayload(queue)),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: QUEUES_KEY });
      qc.invalidateQueries({ queryKey: [...QUEUES_KEY, vars.id] });
    },
  });
}

export function useDeleteQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.deleteRaw(`/queues/${id}`);
      return id;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUEUES_KEY }),
  });
}

export function useDuplicateQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => api.post<Queue>(`/queues/${id}/duplicate`),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUEUES_KEY }),
  });
}

export function useToggleQueueStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => api.post<Queue>(`/queues/${id}/toggle-status`),
    onSuccess: (_d, id) => {
      qc.invalidateQueries({ queryKey: QUEUES_KEY });
      qc.invalidateQueries({ queryKey: [...QUEUES_KEY, id] });
    },
  });
}
