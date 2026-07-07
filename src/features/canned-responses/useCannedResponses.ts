import { useQuery } from "@tanstack/react-query";
import { getCurrentTenantId } from "@/lib/tenant";
import { listCannedResponses, type CannedResponse } from "./api";

export function useCannedResponses() {
  const projectId = getCurrentTenantId();
  return useQuery<CannedResponse[]>({
    queryKey: ["canned-responses", projectId],
    queryFn: () => listCannedResponses(projectId),
    enabled: !!projectId,
    staleTime: 5 * 60 * 1000,
  });
}
