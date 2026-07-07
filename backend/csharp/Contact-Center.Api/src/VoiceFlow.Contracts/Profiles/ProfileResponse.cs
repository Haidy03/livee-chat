namespace VoiceFlow.Contracts.Profiles;

public sealed class ProfileResponse
{
    public DateTime CreatedAt { get; init; }
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public bool BrowserNotifications { get; init; }
    public string Role { get; init; } = string.Empty;
    public List<string> Groups { get; init; } = [];
    public int? ExtensionNumber { get; init; }
    public string? OutboundCid { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool Disabled { get; init; }
    public bool RecordInboundInternal { get; init; }
    public bool RecordInboundExternal { get; init; }
    public bool RecordOutboundInternal { get; init; }
    public bool RecordOutboundExternal { get; init; }
    public bool RecordOnDemand { get; init; }
    public List<ProfileSkillDto>? Skills { get; set; }
    public List<string> AvailableChannels { get; init; } = new();
}

public class ProfileSkillDto
{
    public string Id { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Proficiency { get; set; }

    public int Priority { get; set; }

    public bool Mandatory { get; set; }

    public bool Active { get; set; }
}
