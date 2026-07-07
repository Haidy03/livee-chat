using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Entities.Reports;

[BsonIgnoreExtraElements]
public sealed class ReportRun : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("reportId")]
    public string ReportId { get; set; } = string.Empty;

    [BsonElement("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    [BsonElement("finishedAt")]
    public DateTimeOffset? FinishedAt { get; set; }

    [BsonRepresentation(BsonType.String)]
    [BsonElement("status")]
    public ReportRunStatus Status { get; set; } = ReportRunStatus.Queued;

    [BsonElement("durationMs")]
    public long? DurationMs { get; set; }

    [BsonElement("triggeredBy")]
    public string TriggeredBy { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [BsonElement("trigger")]
    public ReportRunTrigger Trigger { get; set; } = ReportRunTrigger.Manual;

    [BsonElement("rowCount")]
    public int? RowCount { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("resultId")]
    public string? ResultId { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; }
}
