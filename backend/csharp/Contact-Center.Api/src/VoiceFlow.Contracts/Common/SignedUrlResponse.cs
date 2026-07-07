namespace VoiceFlow.Contracts.Common;

public sealed class SignedUrlResponse
{
    public string Url { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
