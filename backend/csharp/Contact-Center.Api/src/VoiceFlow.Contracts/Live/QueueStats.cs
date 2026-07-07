namespace VoiceFlow.Contracts.Live;

public sealed class QueueStats
{
    public int Waiting { get; init; }
    public int LongestWaitSeconds { get; init; }
}
