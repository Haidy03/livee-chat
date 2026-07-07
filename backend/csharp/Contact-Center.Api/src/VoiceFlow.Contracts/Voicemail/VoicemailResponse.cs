namespace VoiceFlow.Contracts.Voicemail;

public sealed class VoicemailResponse
{
    public string Id { get; init; } = string.Empty;
    public string OwnerType { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string? CallerIdNumber { get; init; }
    public string? DestinationNumber { get; init; }
    public int DurationSeconds { get; init; }
    public DateTime Timestamp { get; init; }

    public string? S3Url { get; init; }
    public string? Transcript { get; init; }
    public string? Summary { get; init; }
    public string? Sentiment { get; init; }
    public bool TranscriptionRequested { get; init; }

    public string Status { get; init; } = "new";
    public string? ClaimedBy { get; init; }
    public DateTime? ClaimedAt { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
