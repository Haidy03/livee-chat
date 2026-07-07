namespace VoiceFlow.Contracts.Live;

public sealed class LiveSnapshot
{
    public IReadOnlyList<ActiveCall> ActiveCalls { get; init; } = [];
    public QueueStats Queue { get; init; } = new();
    public IReadOnlyList<LiveAgent> Agents { get; init; } = [];
}
