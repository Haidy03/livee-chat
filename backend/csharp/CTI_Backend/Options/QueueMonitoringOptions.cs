namespace CtiBackend.Options;

public sealed class QueueMonitoringOptions
{
    public const string SectionName = "QueueMonitoring";

    public bool Enabled { get; set; } = true;
    public bool RequestSnapshotOnConnect { get; set; } = true;
    public bool RequestSummaryOnConnect { get; set; } = true;
    public int SnapshotTimeoutSeconds { get; set; } = 30;
    public int SnapshotLockSeconds { get; set; } = 60;
    public int CompletedCallTtlHours { get; set; } = 48;
    public int CompletedWaitingCallerTtlHours { get; set; } = 24;
    public int DeduplicationTtlMinutes { get; set; } = 10;
    public int AmiStaleStateSeconds { get; set; } = 60;
    public bool EnableAdministrativeRefresh { get; set; } = true;
    public string RedisKeyPrefix { get; set; } = "cti";
    public string Environment { get; set; } = "prod";
    public string ServerId { get; set; } = "asterisk-1";
    public string TenantId { get; set; } = "default";
}
