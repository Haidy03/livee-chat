import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  Sparkles,
  Loader2,
  Copy,
  Check,
  ThumbsUp,
  ThumbsDown,
  X,
  FileText,
  Tag,
  Target,
  ListChecks,
  Wand2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAiSuggest } from "@/features/ai-suggest/useAiSuggest";
import type { AiSuggestionType } from "@/features/ai-suggest/api";

interface Props {
  roomId: string;
  currentDraft: string;
  onUseReply: (text: string, suggestionId: string) => void;
  onClose: () => void;
}

const ACTIONS: { type: AiSuggestionType; label: string; icon: React.ReactNode; needsDraft?: boolean }[] = [
  { type: "SuggestedReply", label: "Suggest reply", icon: <Sparkles className="h-3 w-3" /> },
  { type: "ImproveDraft", label: "Improve draft", icon: <Wand2 className="h-3 w-3" />, needsDraft: true },
  { type: "MakeProfessional", label: "More professional", icon: <Wand2 className="h-3 w-3" />, needsDraft: true },
  { type: "MakeShorter", label: "Make shorter", icon: <Wand2 className="h-3 w-3" />, needsDraft: true },
  { type: "ConversationSummary", label: "Summarize", icon: <FileText className="h-3 w-3" /> },
  { type: "IntentDetection", label: "Detect intent", icon: <Target className="h-3 w-3" /> },
  { type: "TagSuggestion", label: "Suggest tags", icon: <Tag className="h-3 w-3" /> },
  { type: "NextBestAction", label: "Next action", icon: <ListChecks className="h-3 w-3" /> },
];

export function AiSuggestPanel({ roomId, currentDraft, onUseReply, onClose }: Props) {
  const { suggestion, generate, loading, error, markUsed, feedback } = useAiSuggest(roomId);
  const [copied, setCopied] = useState<string | null>(null);
  const [fbSent, setFbSent] = useState(false);

  const run = (type: AiSuggestionType, needsDraft?: boolean) => {
    generate({ type, agentDraft: needsDraft ? currentDraft : undefined });
    setFbSent(false);
  };

  const copy = async (text: string) => {
    await navigator.clipboard.writeText(text);
    setCopied(text);
    setTimeout(() => setCopied(null), 1200);
  };

  return (
    <Card className="border-primary/30 bg-primary/[0.02] p-3">
      <div className="mb-2 flex items-center justify-between">
        <div className="flex items-center gap-1.5 text-xs font-semibold text-primary">
          <Sparkles className="h-3.5 w-3.5" /> AI Suggest
        </div>
        <button onClick={onClose} className="text-muted-foreground hover:text-foreground" aria-label="Close">
          <X className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="flex flex-wrap gap-1">
        {ACTIONS.map((a) => (
          <Button
            key={a.type}
            size="sm"
            variant="outline"
            className="h-7 px-2 text-[11px]"
            disabled={loading || (a.needsDraft && !currentDraft.trim())}
            onClick={() => run(a.type, a.needsDraft)}
          >
            <span className="me-1">{a.icon}</span>
            {a.label}
          </Button>
        ))}
      </div>

      <div className="mt-3 min-h-[40px]">
        {loading && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <Loader2 className="h-3.5 w-3.5 animate-spin" /> Thinking…
          </div>
        )}
        {!loading && error && (
          <div className="rounded border border-destructive/40 bg-destructive/5 p-2 text-xs text-destructive">
            {error.message}
          </div>
        )}
        {!loading && !error && suggestion && (
          <div className="space-y-2">
            {suggestion.warning && (
              <div className="rounded border border-amber-400/40 bg-amber-50 p-2 text-[11px] text-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
                {suggestion.warning}
              </div>
            )}

            {suggestion.suggestedReplies.length > 0 && (
              <div className="space-y-1.5">
                {suggestion.suggestedReplies.map((r, i) => (
                  <div key={i} className="rounded border border-app-border bg-background p-2 text-xs">
                    <p className="whitespace-pre-wrap">{r}</p>
                    <div className="mt-1.5 flex items-center gap-1">
                      <Button
                        size="sm"
                        className="h-6 px-2 text-[10px]"
                        onClick={() => onUseReply(r, suggestion.suggestionId)}
                      >
                        Use
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="h-6 px-2 text-[10px]"
                        onClick={() => copy(r)}
                      >
                        {copied === r ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {suggestion.summary && (
              <SummaryOrText label="Summary" text={suggestion.summary} onCopy={copy} copied={copied} />
            )}

            {suggestion.detectedIntent && (
              <div className="rounded border border-app-border bg-background p-2 text-xs">
                <div className="text-[10px] uppercase text-muted-foreground">Intent</div>
                <div className="font-medium">{suggestion.detectedIntent}</div>
              </div>
            )}

            {suggestion.suggestedTags.length > 0 && (
              <div className="rounded border border-app-border bg-background p-2 text-xs">
                <div className="mb-1 text-[10px] uppercase text-muted-foreground">Tags</div>
                <div className="flex flex-wrap gap-1">
                  {suggestion.suggestedTags.map((t) => (
                    <span key={t} className="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] text-primary">
                      {t}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {suggestion.nextActions.length > 0 && (
              <div className="rounded border border-app-border bg-background p-2 text-xs">
                <div className="mb-1 text-[10px] uppercase text-muted-foreground">Next actions</div>
                <ul className="list-disc space-y-0.5 ps-4">
                  {suggestion.nextActions.map((n, i) => (
                    <li key={i}>{n}</li>
                  ))}
                </ul>
              </div>
            )}

            <div className="flex items-center justify-between pt-1 text-[10px] text-muted-foreground">
              <div>
                {suggestion.confidence != null && <>Confidence {Math.round(suggestion.confidence * 100)}%</>}
              </div>
              <div className="flex items-center gap-1">
                <span>Helpful?</span>
                <button
                  className={cn("rounded p-1 hover:bg-muted", fbSent && "opacity-40")}
                  disabled={fbSent}
                  onClick={() => {
                    void feedback(suggestion.suggestionId, "Useful");
                    setFbSent(true);
                  }}
                  aria-label="Useful"
                >
                  <ThumbsUp className="h-3 w-3" />
                </button>
                <button
                  className={cn("rounded p-1 hover:bg-muted", fbSent && "opacity-40")}
                  disabled={fbSent}
                  onClick={() => {
                    void feedback(suggestion.suggestionId, "NotUseful");
                    setFbSent(true);
                  }}
                  aria-label="Not useful"
                >
                  <ThumbsDown className="h-3 w-3" />
                </button>
              </div>
            </div>
          </div>
        )}
        {!loading && !suggestion && !error && (
          <p className="text-[11px] text-muted-foreground">
            Click any action above to get private AI help for this conversation. Nothing is sent to the customer.
          </p>
        )}
      </div>
    </Card>
  );
}

function SummaryOrText({
  label,
  text,
  onCopy,
  copied,
}: {
  label: string;
  text: string;
  onCopy: (t: string) => void;
  copied: string | null;
}) {
  return (
    <div className="rounded border border-app-border bg-background p-2 text-xs">
      <div className="mb-1 flex items-center justify-between">
        <span className="text-[10px] uppercase text-muted-foreground">{label}</span>
        <button className="text-muted-foreground hover:text-foreground" onClick={() => onCopy(text)}>
          {copied === text ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
        </button>
      </div>
      <p className="whitespace-pre-wrap">{text}</p>
    </div>
  );
}

// expose markUsed via hook for the composer
export { useAiSuggest };
