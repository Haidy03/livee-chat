namespace CtiBackend.Services.QueueMonitoring.Models;

public sealed class QueueLiveState
{
    public string TenantId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;

    public string? Strategy { get; set; }
    public int MaxLength { get; set; }

    public int WaitingCount { get; set; }
    public int MemberCount { get; set; }
    public int AvailableAgentCount { get; set; }
    public int RingingAgentCount { get; set; }
    public int TalkingAgentCount { get; set; }
    public int PausedAgentCount { get; set; }
    public int UnavailableAgentCount { get; set; }

    public long Completed { get; set; }
    public long Abandoned { get; set; }

    public DateTime? LastSnapshotUtc { get; set; }
    public DateTime LastEventUtc { get; set; }
    public bool IsStateStale { get; set; }
}
