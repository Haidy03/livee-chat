namespace VoiceFlow.Application.Options;

/// <summary>
/// Read-only monitoring options for Contact-Center. Keys MUST match CTI_Backend
/// so we can read the same Redis state that CTI_Backend writes.
/// </summary>
public sealed class QueueMonitoringOptions
{
    public const string SectionName = "QueueMonitoring";

    public string RedisKeyPrefix { get; set; } = "cti";
    public string Environment { get; set; } = "prod";
    public string DefaultServerId { get; set; } = "asterisk-1";
    public int AmiStaleStateSeconds { get; set; } = 60;
}
