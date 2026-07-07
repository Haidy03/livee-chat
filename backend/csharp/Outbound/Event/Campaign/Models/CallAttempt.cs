using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Outbound.Event.Campaign.Models;

/// <summary>
/// Immutable per-attempt ledger row in the <c>call_attempts</c> collection. One doc per dial;
/// outcome fields are filled in by the outcome handlers as AMI events arrive. Source of truth
/// for reporting / compliance / billing.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class CallAttempt
{
    /// <summary>== ATTEMPT_ID (channelvar / ActionID) — the only correlation key we need.</summary>
    [BsonId]
    public string AttemptId { get; set; } = string.Empty;

    [BsonElement("targetId")] public string TargetId { get; set; } = string.Empty;
    [BsonElement("campaignId")] public string CampaignId { get; set; } = string.Empty;
    [BsonElement("tenantId")] public string TenantId { get; set; } = string.Empty;

    [BsonElement("attemptNumber")] public int AttemptNumber { get; set; }

    [BsonElement("startedAt")] public DateTime StartedAt { get; set; }
    [BsonElement("endedAt"), BsonIgnoreIfNull] public DateTime? EndedAt { get; set; }
    [BsonElement("durationSec"), BsonIgnoreIfNull] public double? DurationSec { get; set; }
    [BsonElement("answeredAt"), BsonIgnoreIfNull] public DateTime? AnsweredAt { get; set; }

    [BsonElement("dialStatus"), BsonIgnoreIfNull] public string? DialStatus { get; set; }
    [BsonElement("hangupCause"), BsonIgnoreIfNull] public string? HangupCause { get; set; }
    [BsonElement("amdStatus"), BsonIgnoreIfNull] public string? AmdStatus { get; set; }
    [BsonElement("amdCause"), BsonIgnoreIfNull] public string? AmdCause { get; set; }
    [BsonElement("queueStatus"), BsonIgnoreIfNull] public string? QueueStatus { get; set; }
    [BsonElement("queueWaitSec"), BsonIgnoreIfNull] public int? QueueWaitSec { get; set; }
    [BsonElement("agentId"), BsonIgnoreIfNull] public string? AgentId { get; set; }
    [BsonElement("trunk"), BsonIgnoreIfNull] public string? Trunk { get; set; }

    [BsonElement("disposition"), BsonIgnoreIfNull] public string? Disposition { get; set; }
    [BsonElement("correlationId")] public string CorrelationId { get; set; } = string.Empty;
}
