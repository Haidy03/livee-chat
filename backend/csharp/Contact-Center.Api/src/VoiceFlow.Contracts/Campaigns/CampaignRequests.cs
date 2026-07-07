using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Campaigns;

public sealed class CreateCampaignRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Type { get; set; } = "outbound_sales";
    public string Status { get; set; } = "draft";
    public string Priority { get; set; } = "medium";
    public string? Description { get; set; }
    public string? Script { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public List<string> AgentIds { get; set; } = new();
    public string? QueueId { get; set; }
    public string AssignedMode { get; set; } = "agents";
    public string DialingMode { get; set; } = "progressive";
    public double? PowerRatio { get; set; }
    /// <summary>Optional initial seed of targets. Stored in the campaign_targets collection.</summary>
    public List<NewCampaignContactDto> Contacts { get; set; } = new();
    public CampaignInboundSettingsDto? InboundSettings { get; set; }
    /// <summary>Optional initial seed of received calls (inbound campaigns).</summary>
    public List<CampaignReceivedCallDto>? ReceivedCalls { get; set; }
}

/// <summary>
/// Request DTO for creating a new campaign contact/target. The server assigns the Id,
/// so this DTO intentionally excludes it to avoid model-validation requiring it.
/// </summary>
public sealed class NewCampaignContactDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "pending";
    public string? LastCallAt { get; set; }
    public string Source { get; set; } = "manual";
}

public sealed class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? Script { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public List<string>? AgentIds { get; set; }
    public string? QueueId { get; set; }
    public string? AssignedMode { get; set; }
    public string? DialingMode { get; set; }
    public double? PowerRatio { get; set; }
    public CampaignInboundSettingsDto? InboundSettings { get; set; }
}

public sealed class SetCampaignStatusRequest
{
    [Required]
    public string Status { get; set; } = "draft";
}

public sealed class AddCampaignContactsRequest
{
    [Required]
    public List<NewCampaignContactDto> Contacts { get; set; } = new();
}

public sealed class UpdateCampaignContactStatusRequest
{
    [Required]
    public string Status { get; set; } = "pending";
}

public sealed class AddCampaignActivityRequest
{
    [Required]
    public string Type { get; set; } = "edited";
    [Required]
    public string Message { get; set; } = string.Empty;
}

public sealed class AddCampaignReceivedCallRequest
{
    [Required]
    public CampaignReceivedCallDto Call { get; set; } = new();
}

public sealed class ListCampaignTargetsRequest
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
