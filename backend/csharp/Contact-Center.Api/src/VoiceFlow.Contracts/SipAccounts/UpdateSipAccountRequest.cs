namespace VoiceFlow.Contracts.SipAccounts;

public sealed class UpdateSipAccountRequest
{
    public string? DisplayName { get; set; }
    public string? WsUrl { get; set; }
    public List<string>? StunUrls { get; set; }
    public string? TurnUrl { get; set; }
    public string? TurnUsername { get; set; }
    public string? AuthId { get; set; }
    public string? SipUri { get; set; }
    public bool? IsActive { get; set; }
}
