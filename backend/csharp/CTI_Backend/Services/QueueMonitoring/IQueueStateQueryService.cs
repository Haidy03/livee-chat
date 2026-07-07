using CtiBackend.Services.QueueMonitoring.Models;

namespace CtiBackend.Services.QueueMonitoring;

public interface IQueueStateQueryService
{
    Task<IReadOnlyCollection<QueueLiveState>> GetQueuesAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueLiveState?> GetQueueAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueAgentLiveState>> GetQueueAgentsAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueWaitingCallerState>> GetWaitingCallersAsync(string tenantId, string serverId, string queue, CancellationToken ct);
    Task<IReadOnlyCollection<QueueAgentLiveState>> GetAgentsAsync(string tenantId, string serverId, CancellationToken ct);
    Task<QueueAgentLiveState?> GetAgentAsync(string tenantId, string serverId, string agentId, CancellationToken ct);
    Task<AmiServerStatus?> GetServerStatusAsync(string tenantId, string serverId, CancellationToken ct);
}

public sealed class QueueStateQueryService : IQueueStateQueryService
{
    private readonly IQueueMonitoringRedisRepository _repo;
    public QueueStateQueryService(IQueueMonitoringRedisRepository repo) => _repo = repo;

    public async Task<IReadOnlyCollection<QueueLiveState>> GetQueuesAsync(string t, string s, CancellationToken ct)
    {
        var names = await _repo.ListQueueNamesAsync(t, s, ct);
        var status = await _repo.GetAmiStatusAsync(t, s, ct);
        var list = new List<QueueLiveState>(names.Count);
        foreach (var n in names)
        {
            var q = await _repo.GetQueueAsync(t, s, n, ct);
            if (q == null) continue;
            q.IsStateStale = status?.IsStateStale ?? false;
            list.Add(q);
        }
        return list;
    }

    public async Task<QueueLiveState?> GetQueueAsync(string t, string s, string q, CancellationToken ct)
    {
        var res = await _repo.GetQueueAsync(t, s, q, ct);
        if (res == null) return null;
        var status = await _repo.GetAmiStatusAsync(t, s, ct);
        res.IsStateStale = status?.IsStateStale ?? false;
        return res;
    }

    public Task<IReadOnlyCollection<QueueAgentLiveState>> GetQueueAgentsAsync(string t, string s, string q, CancellationToken ct)
        => _repo.GetQueueAgentsAsync(t, s, q, ct);

    public Task<IReadOnlyCollection<QueueWaitingCallerState>> GetWaitingCallersAsync(string t, string s, string q, CancellationToken ct)
        => _repo.GetWaitingCallersAsync(t, s, q, ct);

    public async Task<IReadOnlyCollection<QueueAgentLiveState>> GetAgentsAsync(string t, string s, CancellationToken ct)
    {
        var ids = await _repo.ListAgentIdsAsync(t, s, ct);
        var list = new List<QueueAgentLiveState>(ids.Count);
        foreach (var id in ids)
        {
            var a = await _repo.GetAgentAsync(t, s, id, ct);
            if (a != null) list.Add(a);
        }
        return list;
    }

    public Task<QueueAgentLiveState?> GetAgentAsync(string t, string s, string agentId, CancellationToken ct)
        => _repo.GetAgentAsync(t, s, agentId, ct);

    public Task<AmiServerStatus?> GetServerStatusAsync(string t, string s, CancellationToken ct)
        => _repo.GetAmiStatusAsync(t, s, ct);
}
