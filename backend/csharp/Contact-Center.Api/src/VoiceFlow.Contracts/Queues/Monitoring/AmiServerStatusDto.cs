namespace VoiceFlow.Contracts.Queues.Monitoring;

public sealed class AmiServerStatusDto
{
    public string TenantId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public string ConnectionStatus { get; set; } = "Unknown";
    public DateTime? LastConnectedUtc { get; set; }
    public DateTime? LastDisconnectedUtc { get; set; }
    public DateTime? LastEventUtc { get; set; }
    public DateTime? LastSnapshotUtc { get; set; }
    public string SnapshotStatus { get; set; } = "Unknown";
    public bool IsStateStale { get; set; }
    public string? LastError { get; set; }
}
