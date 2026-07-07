using System.Collections.Concurrent;

namespace CtiBackend.Services.QueueMonitoring.Models;

/// <summary>
/// Accumulates QueueStatus / QueueSummary events arriving with a matching
/// ActionID until the corresponding *Complete event arrives.
/// </summary>
public sealed class QueueSnapshotContext
{
    public string ActionId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string ServerId { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;

    public ConcurrentDictionary<string, QueueLiveState> Queues { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentBag<QueueAgentLiveState> Members { get; } = new();
    public ConcurrentBag<QueueWaitingCallerState> WaitingCallers { get; } = new();
}
