namespace VoiceFlow.Contracts.Campaigns;

public sealed class CampaignContactDto
{
    public string? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "pending";
    public string? LastCallAt { get; set; }
    public string Source { get; set; } = "manual";
}

public sealed class CampaignActivityEntryDto
{
    public string? Id { get; set; }
    public string At { get; set; } = string.Empty;
    public string Type { get; set; } = "created";
    public string Message { get; set; } = string.Empty;
}

public sealed class CampaignInboundSettingsDto
{
    public string QueueName { get; set; } = string.Empty;
    public int? ExpectedVolume { get; set; }
    public string? HoursFrom { get; set; }
    public string? HoursTo { get; set; }
    public string? IvrMessage { get; set; }
    public string? OverflowAction { get; set; }
}

public sealed class CampaignReceivedCallDto
{
    public string? Id { get; set; }
    public string CallerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string At { get; set; } = string.Empty;
    public int DurationSec { get; set; }
    public int? WaitSec { get; set; }
    public string? AgentId { get; set; }
    public string Status { get; set; } = "resolved";
    public string? Notes { get; set; }
}

/// <summary>Aggregate target counters, denormalized on the campaign doc.</summary>
public sealed class CampaignTargetCountersDto
{
    public long Total { get; set; }
    public long Pending { get; set; }
    public long Called { get; set; }
    public long Successful { get; set; }
    public long Failed { get; set; }
    public long Callback { get; set; }
}

/// <summary>
/// Campaign metadata only. Contacts, activity, and received calls live in their own
/// collections and are fetched via dedicated paginated endpoints.
/// </summary>
public sealed class CampaignResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Script { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public List<string> AgentIds { get; set; } = new();
    public string? QueueId { get; set; }
    public string AssignedMode { get; set; } = "agents";
    public string DialingMode { get; set; } = "progressive";
    public double PowerRatio { get; set; } = 1.0;
    public CampaignInboundSettingsDto? InboundSettings { get; set; }
    public CampaignTargetCountersDto Targets { get; set; } = new();
    public string? LastActivityAt { get; set; }
    public long Version { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
