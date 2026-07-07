namespace VoiceFlow.Contracts.SipAccounts;

public sealed class SoftphoneCallLogResponse
{
    public string Id { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ContactId { get; init; }
    public DateTime StartedAt { get; init; }
    public int DurationSec { get; init; }
    public string FailureReason { get; init; } = string.Empty;
}
