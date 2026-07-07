using VoiceFlow.Contracts.Queues.Monitoring;

namespace VoiceFlow.Application.Interfaces.QueueMonitoring;

public interface IQueueStateQueryService
{
    Task<IReadOnlyCollection<QueueLiveStateDto>> GetQueuesAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueLiveStateDto?> GetQueueAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueAgentLiveStateDto>> GetQueueAgentsAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueWaitingCallerStateDto>> GetWaitingCallersAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueAgentLiveStateDto>> GetAgentsAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueAgentLiveStateDto?> GetAgentAsync(string tenantId, string serverId, string agentId, CancellationToken ct);
    Task<AmiServerStatusDto?> GetServerStatusAsync(string tenantId, string serverId, CancellationToken ct);
}
