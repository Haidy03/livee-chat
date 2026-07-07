namespace VoiceFlow.Contracts.Queues.Monitoring;

public sealed class QueueWaitingCallerStateDto
{
    public string TenantId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;

    public string CallId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string? LinkedId { get; set; }

    public string QueueName { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? CallerIdNumber { get; set; }
    public string? CallerIdName { get; set; }

    public int Position { get; set; }
    public int? OriginalPosition { get; set; }
    public string Status { get; set; } = "Waiting";

    public DateTime JoinedAtUtc { get; set; }
    public DateTime? ConnectedAtUtc { get; set; }
    public DateTime? LeftAtUtc { get; set; }
    public DateTime? AbandonedAtUtc { get; set; }

    public string? AgentId { get; set; }
    public string? AgentInterface { get; set; }
    public int? HoldTimeSeconds { get; set; }
    public string? LeaveReason { get; set; }

    public DateTime LastEventUtc { get; set; }

    public int WaitingSeconds
    {
        get
        {
            var end = ConnectedAtUtc ?? LeftAtUtc ?? AbandonedAtUtc ?? DateTime.UtcNow;
            return (int)Math.Max(0, (end - JoinedAtUtc).TotalSeconds);
        }
    }
}
