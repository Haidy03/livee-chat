namespace VoiceFlow.Contracts.Live;

/// <summary>In-progress call row for the Live dashboard (string statuses: ringing | answered | hold).</summary>
public sealed class ActiveCall
{
    public string Id { get; init; } = string.Empty;
    public string Caller { get; init; } = string.Empty;
    public string Called { get; init; } = string.Empty;
    public string? AgentId { get; init; }
    public string? GroupId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? AnsweredAt { get; init; }
    public string Status { get; init; } = string.Empty;
}
