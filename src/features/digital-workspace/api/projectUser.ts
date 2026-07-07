import { livechatRequest } from "@/lib/apiClient";

export interface ProjectUserConfig {
  ChatSlots: number;
  clientInactiveTimeout: number; // minutes
  agentInactiveTimeout: number; // minutes
  clientDisconnectedTimeout: number; // minutes
  agentDisconnectedTimeout: number; // minutes
  user_available: boolean;
  chattingType: string | null;
}

/**
 * GET /api/Account/GetProjectUser — LiveChat project/user configuration.
 * Backend currently returns static mocked data.
 */
export async function getProjectUser(): Promise<ProjectUserConfig> {
  const v = await livechatRequest<ProjectUserConfig>("GET", "/Account/GetProjectUser");
  if (!v) throw new Error("Empty project/user config response");
  return v;
}
