using VoiceFlow.Core.Enums;

namespace VoiceFlow.Core.Models;

public sealed class CallAdvancedSearchCriteria
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public IReadOnlyList<CallDirection>? Directions { get; init; }
    public IReadOnlyList<CallStatus>? Statuses { get; init; }
    public IReadOnlyList<(CallDirection Direction, IReadOnlyList<CallStatus> Statuses)>? DirectionStates { get; init; }
    public bool? HasRecording { get; init; }
    public bool? HasVoicemail { get; init; }
    public bool? HasTransfer { get; init; }
    public bool? HasHold { get; init; }
    public IReadOnlyList<Sentiment>? Sentiments { get; init; }
    public bool HangUpByAgent { get; init; }
    public IReadOnlyList<string>? AbandonmentReasons { get; init; }
    public IReadOnlyList<string>? AgentIds { get; init; }
    public IReadOnlyList<string>? GroupIds { get; init; }
    public IReadOnlyList<string>? TagIds { get; init; }
    public string? Caller { get; init; }
    public string? Callee { get; init; }
    public HandledByCriteria HandledBy { get; init; } = HandledByCriteria.Any;
    public string? CallId { get; init; }
    public string? ReferenceId { get; init; }
    public string? Keyword { get; init; }
    public SearchOperatorCriteria SearchOperator { get; init; } = SearchOperatorCriteria.And;
    public DurationRangeCriteria? Duration { get; init; }
    public DurationRangeCriteria? HandlingDuration { get; init; }
    public DurationRangeCriteria? WaitingDuration { get; init; }
    public DurationRangeCriteria? HoldingDuration { get; init; }
    public string SortBy { get; init; } = "startedAt";
    public bool SortDescending { get; init; } = true;
    public int Skip { get; init; }
    public int Take { get; init; }
}

public sealed class DurationRangeCriteria
{
    public int Min { get; init; }
    public int Max { get; init; }
}

public enum HandledByCriteria
{
    Any,
    Agent,
    Ai,
    Ivr
}

public enum SearchOperatorCriteria
{
    And,
    Or,
    Phrase
}
