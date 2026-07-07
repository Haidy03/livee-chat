using System.Collections.Concurrent;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Presence;

/// <summary>
/// Pod-local in-memory presence store used when a real Redis endpoint is not
/// configured. Not safe across multiple replicas — routing / offer distribution
/// will only see agents connected to the same pod. Intended as a stopgap so the
/// AgentHub can accept connections even without Redis.
/// </summary>
public sealed class InMemoryPresenceStore : IPresenceStore
{
    private sealed class Entry
    {
        public AgentStatus Status { get; set; } = AgentStatus.Available;
        public int ActiveChats;
        public int MaxConcurrency = 4;
        public DateTime? LastAssignedAt;
        public List<string> DepartmentIds { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public ConcurrentDictionary<string, byte> Connections { get; } = new();
    }

    private readonly ConcurrentDictionary<string, Entry> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _departments = new(StringComparer.OrdinalIgnoreCase);

    private Entry GetOrCreate(string agentId) => _agents.GetOrAdd(agentId, _ => new Entry());

    public Task HydrateAsync(Agent agent, string connectionId)
    {
        var e = GetOrCreate(agent.Id);
        e.MaxConcurrency = agent.MaxConcurrency;
        e.DepartmentIds = new List<string>(agent.DepartmentIds ?? new());
        e.Languages = new List<string>(agent.Languages ?? new());
        e.Connections[connectionId] = 1;
        foreach (var dept in e.DepartmentIds)
            _departments.GetOrAdd(dept, _ => new(StringComparer.OrdinalIgnoreCase))[agent.Id] = 1;
        return Task.CompletedTask;
    }

    public Task AddConnectionAsync(string agentId, string connectionId)
    {
        GetOrCreate(agentId).Connections[connectionId] = 1;
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string agentId, string connectionId)
    {
        if (_agents.TryGetValue(agentId, out var e))
            e.Connections.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> HasConnectionsAsync(string agentId) =>
        Task.FromResult(_agents.TryGetValue(agentId, out var e) && !e.Connections.IsEmpty);

    public Task<string?> GetAnyConnectionAsync(string agentId) =>
        Task.FromResult(_agents.TryGetValue(agentId, out var e) ? e.Connections.Keys.FirstOrDefault() : null);

    public Task SetStatusAsync(string agentId, AgentStatus status)
    {
        GetOrCreate(agentId).Status = status;
        return Task.CompletedTask;
    }

    public Task<AgentPresence?> GetAsync(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out var e)) return Task.FromResult<AgentPresence?>(null);
        return Task.FromResult<AgentPresence?>(new AgentPresence
        {
            AgentId = agentId,
            Status = e.Status,
            ActiveChats = e.ActiveChats,
            MaxConcurrency = e.MaxConcurrency,
            LastAssignedAt = e.LastAssignedAt,
            DepartmentIds = new List<string>(e.DepartmentIds),
            Languages = new List<string>(e.Languages),
        });
    }

    public Task<int> IncrementLoadAsync(string agentId) =>
        Task.FromResult(Interlocked.Increment(ref GetOrCreate(agentId).ActiveChats));

    public Task<int> DecrementLoadAsync(string agentId)
    {
        var e = GetOrCreate(agentId);
        int next;
        do
        {
            var current = e.ActiveChats;
            if (current <= 0) return Task.FromResult(0);
            next = current - 1;
            if (Interlocked.CompareExchange(ref e.ActiveChats, next, current) == current) break;
        } while (true);
        return Task.FromResult(next);
    }

    public Task TouchLastAssignedAsync(string agentId)
    {
        GetOrCreate(agentId).LastAssignedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<string?> SelectAgentAsync(string departmentId, string lang, IEnumerable<string> excludeAgentIds)
    {
        var exclude = new HashSet<string>(excludeAgentIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        IEnumerable<string> candidateIds;
        if (string.IsNullOrWhiteSpace(departmentId))
        {
            candidateIds = _agents.Keys;
        }
        else
        {
            if (!_departments.TryGetValue(departmentId, out var members)) return Task.FromResult<string?>(null);
            candidateIds = members.Keys;
        }

        AgentPresence? best = null;
        foreach (var id in candidateIds)
        {
            if (exclude.Contains(id)) continue;
            if (!_agents.TryGetValue(id, out var e)) continue;

            var p = new AgentPresence
            {
                AgentId = id,
                Status = e.Status,
                ActiveChats = e.ActiveChats,
                MaxConcurrency = e.MaxConcurrency,
                LastAssignedAt = e.LastAssignedAt,
                Languages = e.Languages,
            };
            if (!p.HasCapacity) continue;
            if (!string.IsNullOrEmpty(lang) && p.Languages.Count > 0
                && !LanguageNormalizer.Matches(p.Languages, lang))
                continue;

            if (best is null
                || p.ActiveChats < best.ActiveChats
                || (p.ActiveChats == best.ActiveChats
                    && (p.LastAssignedAt ?? DateTime.MinValue) < (best.LastAssignedAt ?? DateTime.MinValue)))
            {
                best = p;
            }
        }
        return Task.FromResult(best?.AgentId);
    }
}

public sealed class InMemoryOfferTimeoutStore : IOfferTimeoutStore
{
    private readonly ConcurrentDictionary<string, (string AgentId, long ExpiresAtUnixMs)> _offers = new(StringComparer.Ordinal);

    public Task ArmAsync(string requestId, string agentId, TimeSpan ttl)
    {
        _offers[requestId] = (agentId, DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeMilliseconds());
        return Task.CompletedTask;
    }

    public Task CancelAsync(string requestId)
    {
        _offers.TryRemove(requestId, out _);
        return Task.CompletedTask;
    }

    public Task<List<(string RequestId, string AgentId)>> PopExpiredAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var claimed = new List<(string, string)>();
        foreach (var kv in _offers)
        {
            if (kv.Value.ExpiresAtUnixMs <= now && _offers.TryRemove(kv.Key, out var removed))
                claimed.Add((kv.Key, removed.AgentId));
        }
        return Task.FromResult(claimed);
    }
}
