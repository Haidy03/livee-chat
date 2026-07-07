namespace VoiceFlow.Contracts.Live;

/// <summary>Agent presence for the Live dashboard (string statuses: available | busy | break | offline).</summary>
public sealed class LiveAgent
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int? Extension { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? CurrentCallId { get; init; }
    public DateTime LastChangeAt { get; init; }
}
