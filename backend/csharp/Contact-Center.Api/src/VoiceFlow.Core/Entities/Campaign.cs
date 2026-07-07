using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Campaign : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Lowercase string e.g. "outbound_sales" / "inbound_support".</summary>
    [BsonElement("type")]
    public string Type { get; set; } = "outbound_sales";

    /// <summary>Lowercase string: "draft" | "active" | "paused" | "completed" | "cancelled".</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "draft";

    /// <summary>Lowercase string: "low" | "medium" | "high".</summary>
    [BsonElement("priority")]
    public string Priority { get; set; } = "medium";

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("script")]
    [BsonIgnoreIfNull]
    public string? Script { get; set; }

    [BsonElement("startDate")]
    [BsonIgnoreIfNull]
    public string? StartDate { get; set; }

    [BsonElement("endDate")]
    [BsonIgnoreIfNull]
    public string? EndDate { get; set; }

    [BsonElement("agentIds")]
    public List<string> AgentIds { get; set; } = new();

    [BsonElement("queueId")]
    [BsonIgnoreIfNull]
    public string? QueueId { get; set; }

    /// <summary>"agents" | "queue". Controls whether AgentIds or QueueId is used at runtime.</summary>
    [BsonElement("assignedMode")]
    public string AssignedMode { get; set; } = "agents";


    /// <summary>progressive | power | agentless | predictive | preview | manual.</summary>
    [BsonElement("dialingMode")]
    public string DialingMode { get; set; } = "progressive";

    /// <summary>Lines-per-free-agent for power mode. Ignored unless DialingMode == "power".</summary>
    [BsonElement("powerRatio")]
    public double PowerRatio { get; set; } = 1.0;

    [BsonElement("inboundSettings")]
    [BsonIgnoreIfNull]
    public CampaignInboundSettings? InboundSettings { get; set; }

    // -------- Aggregate counters (kept on the parent doc, mutated with $inc) --------

    [BsonElement("targetsTotal")]
    public long TargetsTotal { get; set; }

    [BsonElement("targetsPending")]
    public long TargetsPending { get; set; }

    [BsonElement("targetsCalled")]
    public long TargetsCalled { get; set; }

    [BsonElement("targetsSuccessful")]
    public long TargetsSuccessful { get; set; }

    [BsonElement("targetsFailed")]
    public long TargetsFailed { get; set; }

    [BsonElement("targetsCallback")]
    public long TargetsCallback { get; set; }

    [BsonElement("lastActivityAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastActivityAt { get; set; }

    /// <summary>Optimistic-concurrency token. Bumped on every mutation of the campaign doc.</summary>
    [BsonElement("version")]
    public long Version { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class CampaignInboundSettings
{
    [BsonElement("queueName")]
    public string QueueName { get; set; } = string.Empty;

    [BsonElement("expectedVolume")]
    [BsonIgnoreIfNull]
    public int? ExpectedVolume { get; set; }

    [BsonElement("hoursFrom")]
    [BsonIgnoreIfNull]
    public string? HoursFrom { get; set; }

    [BsonElement("hoursTo")]
    [BsonIgnoreIfNull]
    public string? HoursTo { get; set; }

    [BsonElement("ivrMessage")]
    [BsonIgnoreIfNull]
    public string? IvrMessage { get; set; }

    /// <summary>"voicemail" | "transfer" | "busy".</summary>
    [BsonElement("overflowAction")]
    [BsonIgnoreIfNull]
    public string? OverflowAction { get; set; }
}

/// <summary>
/// One target/contact for an outbound campaign. Stored in its own collection
/// (<c>campaign_targets</c>) — never embedded on the parent campaign.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class CampaignTarget : Entity, ITenantScoped
{
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

    [BsonElement("email")]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }

    [BsonElement("notes")]
    [BsonIgnoreIfNull]
    public string? Notes { get; set; }

    /// <summary>"pending" | "called" | "successful" | "failed" | "callback".</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("lastCallAt")]
    [BsonIgnoreIfNull]
    public string? LastCallAt { get; set; }

    /// <summary>"manual" | "import" | "directory".</summary>
    [BsonElement("source")]
    public string Source { get; set; } = "manual";
}

/// <summary>Activity log entry for a campaign. Stored in <c>campaign_activity</c>.</summary>
[BsonIgnoreExtraElements]
public sealed class CampaignActivityItem : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [BsonElement("at")]
    public string At { get; set; } = string.Empty;

    /// <summary>"created" | "launched" | "paused" | "resumed" | "edited" | "completed" | "cancelled" | "contacts_added".</summary>
    [BsonElement("type")]
    public string Type { get; set; } = "created";

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Inbound received call for a campaign. Stored in <c>campaign_received_calls</c>.</summary>
[BsonIgnoreExtraElements]
public sealed class CampaignReceivedCallItem : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [BsonElement("callerName")]
    public string CallerName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("at")]
    public string At { get; set; } = string.Empty;

    [BsonElement("durationSec")]
    public int DurationSec { get; set; }

    [BsonElement("waitSec")]
    [BsonIgnoreIfNull]
    public int? WaitSec { get; set; }

    [BsonElement("agentId")]
    [BsonIgnoreIfNull]
    public string? AgentId { get; set; }

    /// <summary>"resolved" | "escalated" | "callback" | "abandoned".</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "resolved";

    [BsonElement("notes")]
    [BsonIgnoreIfNull]
    public string? Notes { get; set; }
}
