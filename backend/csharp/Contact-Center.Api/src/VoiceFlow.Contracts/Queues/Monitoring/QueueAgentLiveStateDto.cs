namespace VoiceFlow.Contracts.Queues.Monitoring;

public sealed class QueueAgentLiveStateDto
{
    public string TenantId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string? QueueName { get; set; }

    public string AgentId { get; set; } = string.Empty;
    public string Interface { get; set; } = string.Empty;
    public string? StateInterface { get; set; }
    public string? MemberName { get; set; }

    public int StatusCode { get; set; }
    public string Status { get; set; } = "Unknown";

    public bool Paused { get; set; }
    public string? PausedReason { get; set; }
    public bool InCall { get; set; }
    public bool RingInUse { get; set; }

    public int Penalty { get; set; }
    public int CallsTaken { get; set; }

    public string? ActiveCallId { get; set; }
    public string? ActiveLinkedId { get; set; }
    public string? ActiveChannel { get; set; }

    public DateTime? RingingSinceUtc { get; set; }
    public DateTime? ConnectedAtUtc { get; set; }
    public DateTime? WrapUpUntilUtc { get; set; }
    public DateTime? LastCallUtc { get; set; }
    public DateTime LastEventUtc { get; set; }
}
