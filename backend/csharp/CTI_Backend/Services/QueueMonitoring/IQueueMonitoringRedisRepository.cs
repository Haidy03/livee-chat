using CtiBackend.Services.QueueMonitoring.Models;

namespace CtiBackend.Services.QueueMonitoring;

public interface IQueueMonitoringRedisRepository
{
    Task UpsertQueueAsync(QueueLiveState queue, CancellationToken ct);
    Task UpsertQueueAgentAsync(QueueAgentLiveState agent, CancellationToken ct);
    Task RemoveQueueAgentAsync(string tenantId, string serverId, string queue, string agentId, CancellationToken ct);

    Task AddWaitingCallerAsync(QueueWaitingCallerState caller, CancellationToken ct);
    Task RemoveWaitingCallerAsync(string tenantId, string serverId, string queue, string callId, string? reason, CancellationToken ct);
    Task<bool> MarkCallerAbandonedAsync(string tenantId, string serverId, string queue, string callId, int? position, int? originalPosition, int? holdTime, CancellationToken ct);
    Task MarkCallerConnectedAsync(string tenantId, string serverId, string queue, string callId, string agentId, string agentInterface, string? agentChannel, int? holdTime, CancellationToken ct);

    Task MarkAgentRingingAsync(string tenantId, string serverId, string queue, string agentId, string agentInterface, string? destChannel, string callId, string? linkedId, CancellationToken ct);
    Task MarkAgentConnectedAsync(string tenantId, string serverId, string queue, string agentId, string agentInterface, string? destChannel, string callId, string? linkedId, int? holdTime, CancellationToken ct);
    Task MarkAgentCompletedAsync(string tenantId, string serverId, string queue, string agentId, string callId, int? talkTime, int? holdTime, string? reason, CancellationToken ct);

    Task<bool> TryAcquireSnapshotLockAsync(string tenantId, string serverId, TimeSpan ttl, string token, CancellationToken ct);
    Task ReleaseSnapshotLockAsync(string tenantId, string serverId, string token, CancellationToken ct);
    Task ApplySnapshotAsync(QueueSnapshotContext ctx, CancellationToken ct);

    Task UpdateAmiStatusAsync(AmiServerStatus status, CancellationToken ct);
    Task<AmiServerStatus?> GetAmiStatusAsync(string tenantId, string serverId, CancellationToken ct);

    Task<bool> TrySetDedupAsync(string tenantId, string serverId, string hash, TimeSpan ttl, CancellationToken ct);

    // Queries
    Task<IReadOnlyCollection<string>> ListQueueNamesAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueLiveState?> GetQueueAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueAgentLiveState>> GetQueueAgentsAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueWaitingCallerState>> GetWaitingCallersAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<string>> ListAgentIdsAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueAgentLiveState?> GetAgentAsync(string tenantId, string serverId, string agentId, CancellationToken ct);
}
