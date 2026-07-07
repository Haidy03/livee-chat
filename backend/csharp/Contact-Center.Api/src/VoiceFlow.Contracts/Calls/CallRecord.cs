using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Calls;

public sealed class CallRecord
{
    public string Id { get; init; } = string.Empty;
    public CallTypeFilter Direction { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public int RingSeconds { get; init; }
    public int HoldSeconds { get; init; }
    public int ActiveSeconds { get; init; }
    public int TotalSeconds { get; init; }
    public string? AgentId { get; init; }
    public string? GroupId { get; init; }
    public string Caller { get; init; } = string.Empty;
    public string Called { get; init; } = string.Empty;
    public List<string> TagIds { get; init; } = [];
    public List<string> AutoTagIds { get; init; } = [];
    public Sentiment? Sentiment { get; init; }
    public HandledByFilter HandledBy { get; init; } = HandledByFilter.Any;
    public string? ReferenceId { get; init; }
    public string Inputs { get; init; } = string.Empty;
    public bool HasRecording { get; init; }
    public List<CallPropertyFilter> Properties { get; init; } = [];
    public string? AbandonmentReason { get; init; }
    public string Notes { get; init; } = string.Empty;
}
