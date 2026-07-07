using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Outbound.Event.Campaign.Models;

/// <summary>
/// Local read model for the <c>campaigns</c> collection (the authoritative entity lives in
/// Contact-Center.Api). Only the fields the Outbound engine needs are mapped.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class CampaignModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>draft | active | paused | completed | cancelled. Dispatcher only acts on "active".</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "draft";

    /// <summary>low | medium | high. Reserved for future fair-share weighting.</summary>
    [BsonElement("priority")]
    public string Priority { get; set; } = "medium";

    [BsonElement("startDate")]
    [BsonIgnoreIfNull]
    public string? StartDate { get; set; }

    [BsonElement("endDate")]
    [BsonIgnoreIfNull]
    public string? EndDate { get; set; }

    /// <summary>progressive (default) | power | agentless | predictive | preview | manual.</summary>
    [BsonElement("dialingMode")]
    [BsonIgnoreIfNull]
    public string? DialingMode { get; set; }

    /// <summary>Power-dial ratio (lines per free agent). Ignored unless dialingMode == "power".</summary>
    [BsonElement("powerRatio")]
    public double PowerRatio { get; set; } = 1.0;

    // --- Predictive dialing knobs (only used when dialingMode == "predictive"; 0 => engine default). ---
    /// <summary>Max acceptable abandonment rate; over-dialing backs off to 1:1 above it. Default 0.03.</summary>
    [BsonElement("abandonRateTarget")]
    public double AbandonRateTarget { get; set; }

    /// <summary>Connect-rate floor so a bad patch can't explode the over-dial ratio. Default 0.20.</summary>
    [BsonElement("minConnectRate")]
    public double MinConnectRate { get; set; }

    /// <summary>Hard ceiling on the over-dial multiplier. Default 3.0.</summary>
    [BsonElement("maxOverdial")]
    public double MaxOverdial { get; set; }

    /// <summary>
    /// "agents" (default) → queue name derived as <c>t_{tenantId}__qc_{campaignId}</c>.
    /// "queue"  → use <see cref="QueueId"/> verbatim (a pre-existing shared queue).
    /// </summary>
    [BsonElement("assignedMode")]
    [BsonIgnoreIfNull]
    public string? AssignedMode { get; set; }

    /// <summary>
    /// Asterisk realtime queue id used in the <c>__OUTBOUND_QUEUE</c> originate variable when
    /// <see cref="AssignedMode"/> == "queue". Ignored when assignedMode == "agents".
    /// </summary>
    [BsonElement("queueId")]
    [BsonIgnoreIfNull]
    public string? QueueId { get; set; }
}
