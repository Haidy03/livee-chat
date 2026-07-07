using System.Text;
using System.Text.Json;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.AiSuggestions;

public interface IAiSuggestionPromptBuilder
{
    string SystemPrompt { get; }
    string PromptVersion { get; }
    string BuildUserPrompt(AiSuggestionType type, AiConversationContext ctx, string? agentDraft, string? targetLanguage);
}

public sealed class AiSuggestionPromptBuilder : IAiSuggestionPromptBuilder
{
    public string PromptVersion => "v1";

    public string SystemPrompt =>
@"You are an AI assistant helping a human live chat agent.
You do not talk directly to the customer.
You only suggest responses, summaries, tags, intents, or next actions to the agent.
Be concise, accurate, and professional.
Use the same language as the customer unless the agent requests another language.
Do not invent facts.
If information is missing, suggest what the agent should ask.
Return valid JSON only.";

    public string BuildUserPrompt(AiSuggestionType type, AiConversationContext ctx, string? agentDraft, string? targetLanguage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Conversation context:");
        sb.AppendLine(JsonSerializer.Serialize(new
        {
            channel = ctx.Channel,
            customerLanguage = ctx.Lang,
            department = ctx.DepartmentName,
            agent = ctx.CurrentAgentName,
            messages = ctx.Messages.Select(m => new { sender = m.SenderType, text = m.Text, at = m.SentAtUtc }),
        }));
        sb.AppendLine();

        switch (type)
        {
            case AiSuggestionType.SuggestedReply:
                sb.AppendLine("Task: Propose 1-3 concise reply options the agent could send.");
                sb.AppendLine("Respond with JSON: { \"suggestedReplies\": string[], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.ConversationSummary:
                sb.AppendLine("Task: Summarize the conversation for a joining agent in 2-3 sentences.");
                sb.AppendLine("Respond with JSON: { \"summary\": string, \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.IntentDetection:
                sb.AppendLine("Task: Detect the customer's primary intent and suggest 1-4 relevant tags.");
                sb.AppendLine("Respond with JSON: { \"detectedIntent\": string, \"suggestedTags\": string[], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.TagSuggestion:
                sb.AppendLine("Task: Suggest 1-5 tags for this conversation.");
                sb.AppendLine("Respond with JSON: { \"suggestedTags\": string[], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.NextBestAction:
                sb.AppendLine("Task: Suggest 1-4 next actions the agent should take.");
                sb.AppendLine("Respond with JSON: { \"nextActions\": string[], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.ImproveDraft:
                sb.AppendLine($"Agent draft to improve: \"{agentDraft ?? string.Empty}\"");
                sb.AppendLine("Task: Rewrite the draft to be clearer, warmer, and grammatically correct while keeping meaning.");
                sb.AppendLine("Respond with JSON: { \"suggestedReplies\": [string], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.MakeProfessional:
                sb.AppendLine($"Agent draft: \"{agentDraft ?? string.Empty}\"");
                sb.AppendLine("Task: Rewrite in a more professional tone.");
                sb.AppendLine("Respond with JSON: { \"suggestedReplies\": [string], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.MakeShorter:
                sb.AppendLine($"Agent draft: \"{agentDraft ?? string.Empty}\"");
                sb.AppendLine("Task: Rewrite the draft to be much shorter while keeping key info.");
                sb.AppendLine("Respond with JSON: { \"suggestedReplies\": [string], \"confidence\": number, \"warning\": string|null }");
                break;
            case AiSuggestionType.Translate:
                sb.AppendLine($"Agent draft: \"{agentDraft ?? string.Empty}\"");
                sb.AppendLine($"Task: Translate to '{targetLanguage ?? "en"}' preserving tone.");
                sb.AppendLine("Respond with JSON: { \"suggestedReplies\": [string], \"confidence\": number, \"warning\": string|null }");
                break;
        }
        return sb.ToString();
    }
}
