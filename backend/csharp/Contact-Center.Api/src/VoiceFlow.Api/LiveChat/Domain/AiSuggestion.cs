using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Domain;

public enum AiSuggestionType
{
    SuggestedReply,
    ConversationSummary,
    IntentDetection,
    TagSuggestion,
    NextBestAction,
    ImproveDraft,
    MakeProfessional,
    MakeShorter,
    Translate,
}

public enum AiSuggestionStatus
{
    Pending,
    Completed,
    Failed,
}

public enum AiSuggestionFeedback
{
    Useful,
    NotUseful,
}

[BsonIgnoreExtraElements]
public class AiSuggestion
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string projectId { get; set; } = string.Empty;
    public string roomId { get; set; } = string.Empty;
    public string conversationId { get; set; } = string.Empty;
    public string requestedByAgentId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public AiSuggestionType type { get; set; }

    [BsonRepresentation(BsonType.String)]
    public AiSuggestionStatus status { get; set; } = AiSuggestionStatus.Pending;

    public string? agentDraft { get; set; }
    public string? targetLanguage { get; set; }

    public List<string> suggestedReplies { get; set; } = new();
    public string? summary { get; set; }
    public string? detectedIntent { get; set; }
    public List<string> suggestedTags { get; set; } = new();
    public List<string> nextActions { get; set; } = new();

    public decimal? confidence { get; set; }
    public string? warning { get; set; }
    public string? errorMessage { get; set; }
    public string promptVersion { get; set; } = "v1";

    public DateTime createdAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? usedAtUtc { get; set; }
    public string? usedSuggestionText { get; set; }
    public string? sentMessageId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public AiSuggestionFeedback? feedback { get; set; }
    public string? feedbackComment { get; set; }
    public DateTime? feedbackAtUtc { get; set; }
}
