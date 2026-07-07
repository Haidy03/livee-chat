using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.Surveys
{

    public class SurveyResponse: Entity, ITenantScoped
    {
        [BsonElement("tenantId")]
        public string TenantId { get; set; } = string.Empty;
        [BsonElement("surveyId")]
        public string? SurveyId { get; set; }

        [BsonElement("callerPhone")]
        public string CallerPhone { get; set; } = "";

        [BsonElement("at")]
        public DateTimeOffset At { get; set; }

        [BsonElement("completed")]
        public bool Completed { get; set; }

        [BsonElement("answers")]
        public Dictionary<string, string?> Answers { get; set; } = new();
        [BsonElement("callId")]
        public string? CallId { get; set; }

        [BsonElement("startedAt")]
        public DateTimeOffset? StartedAt { get; set; }

        [BsonElement("endedAt")]
        public DateTimeOffset? EndedAt { get; set; }

        [BsonElement("durationSeconds")]
        public int? DurationSeconds { get; set; }
        [BsonElement("completionStatus")]
        public string? CompletionStatus { get; set; }

        [BsonElement("language")]
        public string? Language { get; set; }
        [BsonElement("customFields")]
        public Dictionary<string, string> CustomFields { get; set; } = new();
        [BsonElement("passedVariables")]
        public Dictionary<string, string> PassedVariables { get; set; } = new();
        
    }
}
