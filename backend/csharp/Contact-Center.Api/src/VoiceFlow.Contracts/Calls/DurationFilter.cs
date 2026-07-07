namespace VoiceFlow.Contracts.Calls;

public sealed class DurationFilter
{
    public bool Enabled { get; set; }
    public int Min { get; set; }
    public int Max { get; set; } = 300;
}
