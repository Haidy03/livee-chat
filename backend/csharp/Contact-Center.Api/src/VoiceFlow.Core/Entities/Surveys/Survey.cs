using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;
using VoiceFlow.Core.Exceptions.Surveys;

namespace VoiceFlow.Core.Entities.Surveys;

public class Survey: Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("description")]
    public string Description { get; set; } = "";

    [BsonElement("language")]
    [BsonRepresentation(BsonType.String)]
    public SurveyLanguage Language { get; set; }

    [BsonElement("ttsVoice")]
    [BsonRepresentation(BsonType.String)]
    public TtsVoice TtsVoice { get; set; }

    [BsonElement("welcomeSound")]
    public SoundFileInfo? welcomeSound { get; set; }

    [BsonElement("completionSound")]
    public SoundFileInfo? completionSound { get; set; }

    [BsonElement("webhookUrl")]
    public string WebhookUrl { get; set; } = "";

    [BsonElement("webhookSecret")]
    [BsonIgnoreIfNull]
    public string? WebhookSecret { get; set; }

    [BsonElement("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [BsonElement("inputTimeoutSec")]
    public int InputTimeoutSec { get; set; } = 5;

    [BsonElement("abandonment")]
    [BsonRepresentation(BsonType.String)]
    public Abandonment? Abandonment { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    [BsonElement("questions")]
    public List<Question> Questions { get; set; } = new();

    [BsonElement("usedInFlowIds")]
    public List<Guid> UsedInFlowIds { get; set; } = new();


    public void SetStatus(SurveyStatus next)
    {
        // Allow any transition between draft/published/archived for now.
        Status = next;
    }

    public void ValidateBranching()
    {
        var ids = new HashSet<string>(Questions.Select(q => q.Id));
        foreach (var q in Questions)
            foreach (var r in q.BranchingRules)
                if (r.Action is GotoBranchAction g && !ids.Contains(g.QuestionId))
                    throw new BranchTargetMissingException(g.QuestionId);
    }

    public Survey Duplicate(Guid newId, DateTimeOffset now) => new()
    {
        Id = newId.ToString(),
        TenantId = TenantId,
        Name = Name + " (copy)",
        Description = Description,
        Language = Language,
        TtsVoice = TtsVoice,
        welcomeSound = welcomeSound,
        completionSound = completionSound,
        WebhookUrl = WebhookUrl,
        WebhookSecret = WebhookSecret,
        MaxRetries = MaxRetries,
        InputTimeoutSec = InputTimeoutSec,
        Abandonment = Abandonment,
        Status = SurveyStatus.Draft,
        CreatedAt = now.UtcDateTime,
        UpdatedAt = now.UtcDateTime,
        Questions = Questions.Select(q => new Question
        {
            Id = q.Id, Order = q.Order, Type = q.Type, Text = q.Text,
            Required = q.Required, Config = q.Config,
            BranchingRules = q.BranchingRules.ToList(),
        }).ToList(),
        UsedInFlowIds = new(),
    };
}

