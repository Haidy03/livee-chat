import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";
import { getCurrentTenantId } from "@/lib/tenant";

export interface PipelineUser {
  id: string;
  name: string;
  extension: string;
}

export function usePipelineUsers() {
  return useQuery({
    queryKey: ["softphone-pipelines"],
    queryFn: async (): Promise<PipelineUser[]> => {
      let tid: string | null = null;
      try { tid = getCurrentTenantId(); } catch { tid = null; }
      if (!tid) return [];
      const res = await api.get<any>(`/voicebot/projects/${tid}/pipelines`);
      const rows: any[] = Array.isArray(res) ? res : (res?.items ?? []);
      return rows
        .filter((r) => r?.active === true && r?.extension)
        .map((r) => ({
          id: String(r.id),
          name: r.name ?? "Pipeline",
          extension: String(r.extension),
        }));
    },
    staleTime: 60_000,
  });
}
