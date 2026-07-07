using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Outbound.Event.Campaign.Models;

/// <summary>
/// Local model for the <c>campaign_targets</c> collection. Pull-based lifecycle:
/// pending → dialing → successful | callback | failed. Retries reset to <c>pending</c> with
/// <see cref="NextAttemptAt"/> as the release gate — there is no intermediate <c>queued</c>
/// stage (HOLD/MAIN are gone).
/// </summary>
[BsonIgnoreExtraElements]
public sealed class CampaignTarget
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>pending | dialing | successful | failed | callback.</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("source")]
    public string Source { get; set; } = "manual";

    // -------- Dispatch / retry bookkeeping (added by the Outbound engine) --------

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    /// <summary>Backoff gate — the target is not eligible for claim before this time.</summary>
    [BsonElement("nextAttemptAt")]
    [BsonIgnoreIfNull]
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>When the dispatcher last claimed this target (pending → dialing).</summary>
    [BsonElement("dialingAt")]
    [BsonIgnoreIfNull]
    public DateTime? DialingAt { get; set; }

    /// <summary>Correlation id for the in-flight attempt (matches CallAttempt.AttemptId).</summary>
    [BsonElement("attemptId")]
    [BsonIgnoreIfNull]
    public string? AttemptId { get; set; }

    [BsonElement("lastDisposition")]
    [BsonIgnoreIfNull]
    public string? LastDisposition { get; set; }

    [BsonElement("lastError")]
    [BsonIgnoreIfNull]
    public string? LastError { get; set; }
}
