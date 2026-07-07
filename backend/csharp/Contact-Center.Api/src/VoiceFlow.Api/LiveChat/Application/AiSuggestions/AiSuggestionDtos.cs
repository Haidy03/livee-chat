using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.AiSuggestions;

public class AiSuggestionRequest
{
    public string RoomId { get; set; } = string.Empty;
    public AiSuggestionType Type { get; set; }
    public string? AgentDraft { get; set; }
    public string? TargetLanguage { get; set; }
}

public class AiSuggestionResponse
{
    public string SuggestionId { get; set; } = string.Empty;
    public AiSuggestionType Type { get; set; }
    public List<string> SuggestedReplies { get; set; } = new();
    public string? Summary { get; set; }
    public string? DetectedIntent { get; set; }
    public List<string> SuggestedTags { get; set; } = new();
    public List<string> NextActions { get; set; } = new();
    public decimal? Confidence { get; set; }
    public string? Warning { get; set; }
}

public class AiSuggestionFeedbackRequest
{
    public string SuggestionId { get; set; } = string.Empty;
    public AiSuggestionFeedback Feedback { get; set; }
    public string? Comment { get; set; }
}

public class AiSuggestionMarkUsedRequest
{
    public string UsedText { get; set; } = string.Empty;
    public string? SentMessageId { get; set; }
}

public class AiConversationContext
{
    public string ProjectId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Channel { get; set; }
    public string? Lang { get; set; }
    public List<AiMessageContextItem> Messages { get; set; } = new();
    public string? CurrentAgentName { get; set; }
    public string? DepartmentName { get; set; }
}

public class AiMessageContextItem
{
    public string SenderType { get; set; } = string.Empty; // Customer, Agent, Bot, System
    public string? SenderName { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
