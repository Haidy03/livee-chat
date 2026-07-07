using System.Globalization;
using StackExchange.Redis;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Redis;

public sealed class PresenceStore : IPresenceStore
{
    private readonly IConnectionMultiplexer _mux;

    // Floors HINCRBY at zero so declines/timeouts can't push load negative.
    private static readonly string DecrScript =
        "local v = tonumber(redis.call('HGET', KEYS[1], ARGV[1]) or '0');" +
        "if v <= 0 then redis.call('HSET', KEYS[1], ARGV[1], 0); return 0 end;" +
        "return redis.call('HINCRBY', KEYS[1], ARGV[1], -1);";

    public PresenceStore(IConnectionMultiplexer mux) => _mux = mux;
    private IDatabase Db => _mux.GetDatabase();

    public async Task HydrateAsync(Agent agent, string connectionId)
    {
        var db = Db;
        var key = RedisKeys.Presence(agent.Id);

        // Static profile: always refresh from Mongo source of truth.
        var staticEntries = new HashEntry[]
        {
            new(RedisKeys.HMax, agent.MaxConcurrency),
            new(RedisKeys.HDepts, string.Join(",", agent.DepartmentIds)),
            new(RedisKeys.HLangs, string.Join(",", agent.Languages)),
        };
        await db.HashSetAsync(key, staticEntries);

        // Live counters: initialize only if missing (reconnect must not clobber).
        if (!await db.HashExistsAsync(key, RedisKeys.HActive))
            await db.HashSetAsync(key, RedisKeys.HActive, 0);
        if (!await db.HashExistsAsync(key, RedisKeys.HStatus))
            await db.HashSetAsync(key, RedisKeys.HStatus, AgentStatus.Available.ToString());

        // Department index
        foreach (var dept in agent.DepartmentIds)
            await db.SetAddAsync(RedisKeys.Dept(dept), agent.Id);

        await AddConnectionAsync(agent.Id, connectionId);
    }

    public Task AddConnectionAsync(string agentId, string connectionId) =>
        Db.SetAddAsync(RedisKeys.Conns(agentId), connectionId);

    public Task RemoveConnectionAsync(string agentId, string connectionId) =>
        Db.SetRemoveAsync(RedisKeys.Conns(agentId), connectionId);

    public async Task<bool> HasConnectionsAsync(string agentId) =>
        (await Db.SetLengthAsync(RedisKeys.Conns(agentId))) > 0;

    public async Task<string?> GetAnyConnectionAsync(string agentId)
    {
        var v = await Db.SetRandomMemberAsync(RedisKeys.Conns(agentId));
        return v.IsNullOrEmpty ? null : v.ToString();
    }

    public Task SetStatusAsync(string agentId, AgentStatus status) =>
        Db.HashSetAsync(RedisKeys.Presence(agentId), RedisKeys.HStatus, status.ToString());

    public async Task<AgentPresence?> GetAsync(string agentId)
    {
        var entries = await Db.HashGetAllAsync(RedisKeys.Presence(agentId));
        if (entries.Length == 0) return null;
        var map = entries.ToDictionary(e => (string)e.Name!, e => e.Value);

        var presence = new AgentPresence { AgentId = agentId };
        if (map.TryGetValue(RedisKeys.HStatus, out var s) && Enum.TryParse<AgentStatus>(s.ToString(), out var st))
            presence.Status = st;
        if (map.TryGetValue(RedisKeys.HActive, out var a) && int.TryParse(a.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ai))
            presence.ActiveChats = ai;
        if (map.TryGetValue(RedisKeys.HMax, out var m) && int.TryParse(m.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi))
            presence.MaxConcurrency = mi;
        if (map.TryGetValue(RedisKeys.HLast, out var l) && long.TryParse(l.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lts))
            presence.LastAssignedAt = DateTimeOffset.FromUnixTimeMilliseconds(lts).UtcDateTime;
        if (map.TryGetValue(RedisKeys.HDepts, out var d))
            presence.DepartmentIds = d.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (map.TryGetValue(RedisKeys.HLangs, out var lg))
            presence.Languages = lg.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        return presence;
    }

    public async Task<int> IncrementLoadAsync(string agentId)
    {
        var v = await Db.HashIncrementAsync(RedisKeys.Presence(agentId), RedisKeys.HActive, 1);
        return (int)v;
    }

    public async Task<int> DecrementLoadAsync(string agentId)
    {
        var v = (long)await Db.ScriptEvaluateAsync(DecrScript,
            new RedisKey[] { RedisKeys.Presence(agentId) },
            new RedisValue[] { RedisKeys.HActive });
        return (int)v;
    }

    public Task TouchLastAssignedAsync(string agentId) =>
        Db.HashSetAsync(RedisKeys.Presence(agentId), RedisKeys.HLast,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    public async Task<string?> SelectAgentAsync(string departmentId, string lang, IEnumerable<string> excludeAgentIds)
    {
        var db = Db;
        var exclude = new HashSet<string>(excludeAgentIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        IEnumerable<string> candidateIds;
        if (string.IsNullOrWhiteSpace(departmentId))
        {
            // No department on the request — consider all known agents.
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var prefix = RedisKeys.Presence(string.Empty); // "livechat:presence:"
            foreach (var endpoint in _mux.GetEndPoints())
            {
                var server = _mux.GetServer(endpoint);
                if (server.IsReplica) continue;
                foreach (var key in server.Keys(db.Database, pattern: prefix + "*", pageSize: 250))
                {
                    var k = key.ToString();
                    if (k.StartsWith(prefix, StringComparison.Ordinal))
                        ids.Add(k.Substring(prefix.Length));
                }
            }
            candidateIds = ids;
        }
        else
        {
            var members = await db.SetMembersAsync(RedisKeys.Dept(departmentId));
            candidateIds = members.Select(m => m.ToString());
        }

        AgentPresence? best = null;
        foreach (var id in candidateIds)
        {

            if (exclude.Contains(id)) continue;
            var p = await GetAsync(id);
            if (p is null || !p.HasCapacity) continue;
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
        return best?.AgentId;
    }
}
