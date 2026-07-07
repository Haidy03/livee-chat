using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Entities.Reports
{
    [BsonIgnoreExtraElements]
    public sealed class Report : Entity, ITenantScoped
    {
        [BsonElement("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("name")]
        public Bi Name { get; set; } = new();

        [BsonElement("description")]
        public Bi Description { get; set; } = new();

        [BsonRepresentation(BsonType.String)]
        [BsonElement("category")]
        public ReportCategory Category { get; set; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("type")]
        public ReportType Type { get; set; }

        [BsonElement("ownerId")]
        public string OwnerId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        [BsonElement("status")]
        public ReportStatus Status { get; set; } = ReportStatus.Draft;

        [BsonElement("definition")]
        public ReportDefinition Definition { get; set; } = new();

        [BsonElement("schedule")]
        public Schedule Schedule { get; set; } = new();

        [BsonElement("nextRunAt")]
        public DateTimeOffset? NextRunAt { get; set; }

        [BsonElement("starred")]
        public bool Starred { get; set; }

        [BsonElement("lastRunAt")]
        public DateTimeOffset? LastRunAt { get; set; }

        [BsonElement("runsCount")]
        public int RunsCount { get; set; }

        [BsonElement("recipientsCount")]
        public int RecipientsCount { get; set; }
    }

}
