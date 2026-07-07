import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/apiClient";

export type VoicemailStatus = "new" | "claimed" | "done";

export interface Voicemail {
  id: string;
  ownerType: string;
  ownerId: string;
  callerIdNumber?: string;
  destinationNumber?: string;
  durationSeconds: number;
  timestamp: string;
  s3Url?: string;
  transcript?: string;
  summary?: string;
  sentiment?: string;
  transcriptionRequested: boolean;
  status: VoicemailStatus;
  claimedBy?: string;
  claimedAt?: string;
  resolvedBy?: string;
  resolvedAt?: string;
}

const VOICEMAILS_KEY = ["voicemails"] as const;

export function useVoicemails(status?: VoicemailStatus) {
  return useQuery({
    queryKey: [...VOICEMAILS_KEY, status ?? "all"],
    queryFn: async (): Promise<Voicemail[]> => {
      const rows = await api.get<Voicemail[]>(
        `/voicemails${status ? `?status=${status}` : ""}`,
      );
      return rows ?? [];
    },
    refetchInterval: 15000, // light polling until the realtime badge is wired
  });
}

export function useClaimVoicemail() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post(`/voicemails/${id}/claim`),
    onSuccess: () => qc.invalidateQueries({ queryKey: VOICEMAILS_KEY }),
  });
}

export function useResolveVoicemail() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post(`/voicemails/${id}/resolve`),
    onSuccess: () => qc.invalidateQueries({ queryKey: VOICEMAILS_KEY }),
  });
}
