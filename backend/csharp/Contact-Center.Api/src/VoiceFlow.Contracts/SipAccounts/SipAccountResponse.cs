namespace VoiceFlow.Contracts.SipAccounts;

public sealed class SipAccountResponse
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SipUri { get; init; } = string.Empty;
    public string AuthId { get; init; } = string.Empty;
    public string WsUrl { get; init; } = string.Empty;
    public List<string> StunUrls { get; init; } = [];
    public string TurnUrl { get; init; } = string.Empty;
    public string PPXPassword { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
