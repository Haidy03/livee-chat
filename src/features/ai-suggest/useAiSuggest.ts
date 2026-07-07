import { useState, useCallback } from "react";
import { useMutation } from "@tanstack/react-query";
import { getCurrentTenantId } from "@/lib/tenant";
import { useToast } from "@/hooks/use-toast";
import {
  generateSuggestion,
  markSuggestionUsed,
  sendSuggestionFeedback,
  type AiSuggestionResponse,
  type AiSuggestionType,
  type AiSuggestionFeedback,
} from "./api";

export function useAiSuggest(roomId: string) {
  const projectId = getCurrentTenantId();
  const { toast } = useToast();
  const [suggestion, setSuggestion] = useState<AiSuggestionResponse | null>(null);

  const gen = useMutation({
    mutationFn: (args: { type: AiSuggestionType; agentDraft?: string; targetLanguage?: string }) =>
      generateSuggestion(projectId, {
        roomId,
        type: args.type,
        agentDraft: args.agentDraft,
        targetLanguage: args.targetLanguage,
      }),
    onSuccess: (data) => setSuggestion(data),
    onError: (e: Error) => {
      toast({ title: "AI Suggest failed", description: e.message, variant: "destructive" });
    },
  });

  const markUsed = useCallback(
    async (suggestionId: string, usedText: string, sentMessageId?: string | null) => {
      try {
        await markSuggestionUsed(projectId, suggestionId, usedText, sentMessageId);
      } catch {
        /* non-fatal */
      }
    },
    [projectId],
  );

  const feedback = useCallback(
    async (suggestionId: string, value: AiSuggestionFeedback, comment?: string) => {
      try {
        await sendSuggestionFeedback(projectId, suggestionId, value, comment);
        toast({ title: "Thanks for the feedback" });
      } catch (e) {
        toast({ title: "Could not save feedback", variant: "destructive" });
      }
    },
    [projectId, toast],
  );

  return {
    suggestion,
    setSuggestion,
    generate: gen.mutate,
    generateAsync: gen.mutateAsync,
    loading: gen.isPending,
    error: gen.error as Error | null,
    markUsed,
    feedback,
  };
}
