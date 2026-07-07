import { api, decodeToken } from "@/lib/apiClient";

export interface WrapUpInput {
  sipCallId: string;
  disposition: string;
  notes: string;
  callbackScheduled: boolean;
  acwSeconds: number;
}

export async function saveWrapUp(input: WrapUpInput): Promise<void> {
  const agentId = decodeToken()?.sub ?? "";
  const payload = {
    sipCallId: input.sipCallId,
    wrapUp: {
      disposition: input.disposition,
      notes: input.notes,
      callbackScheduled: input.callbackScheduled,
      acwSeconds: input.acwSeconds,
      completedAt: new Date().toISOString(),
      agentId,
      status: "wrapped",
    },
  };
  await api.post("/calls/wrap-up", payload);
}
