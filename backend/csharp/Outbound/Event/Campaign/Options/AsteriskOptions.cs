namespace Outbound.Event.Campaign.Options;

public sealed class AsteriskOptions
{
    public const string SectionName = "Asterisk";

    /// <summary>How long Asterisk rings the channel before treating the originate as no-answer.</summary>
    public int RingTimeoutSeconds { get; set; } = 30;

    /// <summary>Extra time the action waits past the ring window before bailing out with no_answer.</summary>
    public int OutcomeBufferSeconds { get; set; } = 5;
}
