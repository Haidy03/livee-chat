using VoiceFlow.Core.Enums.Hubspot;

namespace VoiceFlow.Contracts.Hubspot;

public sealed class HubSpotIntegrationStatusResponse
{
    public string Provider { get; set; } = "hubspot";
    public bool Connected { get; set; }
    public string Status { get; set; } = nameof(HubSpotIntegrationStatus.Disconnected);
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public List<string> GrantedScopes { get; set; } = new();
    public DateTime? ConnectedAtUtc { get; set; }
    public string? ConnectedByUserId { get; set; }
    public DateTime? LastRefreshedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
}
