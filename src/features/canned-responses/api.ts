import { livechatRequest } from "@/lib/apiClient";

export interface CannedResponse {
  _id: string;
  projectId: string;
  title: string;
  messages: string[];
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface CannedResponseInput {
  title: string;
  messages: string[];
}

const base = (projectId: string) => `/${projectId}/CannedResponses`;

async function unwrap<T>(p: Promise<T | undefined>): Promise<T> {
  const v = await p;
  if (v === undefined) throw new Error("Empty API response");
  return v;
}

export function listCannedResponses(projectId: string) {
  return unwrap(livechatRequest<CannedResponse[]>("GET", `${base(projectId)}/all`));
}

export function createCannedResponse(projectId: string, payload: CannedResponseInput) {
  return unwrap(livechatRequest<CannedResponse>("POST", base(projectId), payload));
}

export function updateCannedResponse(projectId: string, id: string, payload: CannedResponseInput) {
  return unwrap(livechatRequest<CannedResponse>("PUT", `${base(projectId)}/${id}`, payload));
}

export async function deleteCannedResponse(projectId: string, id: string) {
  await livechatRequest("DELETE", `${base(projectId)}/${id}`);
}
