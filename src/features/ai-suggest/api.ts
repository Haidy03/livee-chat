import { livechatRequest } from "@/lib/apiClient";

export type AiSuggestionType =
  | "SuggestedReply"
  | "ConversationSummary"
  | "IntentDetection"
  | "TagSuggestion"
  | "NextBestAction"
  | "ImproveDraft"
  | "MakeProfessional"
  | "MakeShorter"
  | "Translate";

export type AiSuggestionFeedback = "Useful" | "NotUseful";

export interface AiSuggestionResponse {
  suggestionId: string;
  type: AiSuggestionType;
  suggestedReplies: string[];
  summary?: string | null;
  detectedIntent?: string | null;
  suggestedTags: string[];
  nextActions: string[];
  confidence?: number | null;
  warning?: string | null;
}

export interface GenerateRequest {
  roomId: string;
  type: AiSuggestionType;
  agentDraft?: string | null;
  targetLanguage?: string | null;
}

const base = (projectId: string) => `/${projectId}/AiSuggestions`;

async function unwrap<T>(p: Promise<T | undefined>): Promise<T> {
  const v = await p;
  if (v === undefined) throw new Error("Empty API response");
  return v;
}

export function generateSuggestion(projectId: string, body: GenerateRequest) {
  return unwrap(livechatRequest<AiSuggestionResponse>("POST", `${base(projectId)}/generate`, body));
}

export async function markSuggestionUsed(
  projectId: string,
  suggestionId: string,
  usedText: string,
  sentMessageId?: string | null,
) {
  await livechatRequest("POST", `${base(projectId)}/${suggestionId}/used`, { usedText, sentMessageId });
}

export async function sendSuggestionFeedback(
  projectId: string,
  suggestionId: string,
  feedback: AiSuggestionFeedback,
  comment?: string,
) {
  await livechatRequest("POST", `${base(projectId)}/feedback`, { suggestionId, feedback, comment });
}
