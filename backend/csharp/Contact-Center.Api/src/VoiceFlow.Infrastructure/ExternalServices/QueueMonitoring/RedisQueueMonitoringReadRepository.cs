using System.Globalization;
using HelperLib.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using VoiceFlow.Application.Interfaces.QueueMonitoring;
using VoiceFlow.Contracts.Queues.Monitoring;

namespace VoiceFlow.Infrastructure.ExternalServices.QueueMonitoring;

/// <summary>
/// Read-only Redis-backed implementation. Mirrors the read paths of CTI_Backend's
/// QueueMonitoringRedisRepository against the same key layout.
/// </summary>
public sealed class RedisQueueMonitoringReadRepository : IQueueMonitoringReadRepository
{
    private readonly RedisSentinelConnectionFactory _redis;
    private readonly QueueMonitoringKeys _keys;
    private readonly ILogger<RedisQueueMonitoringReadRepository> _log;

    public RedisQueueMonitoringReadRepository(
        RedisSentinelConnectionFactory redis,
        QueueMonitoringKeys keys,
        ILogger<RedisQueueMonitoringReadRepository> log)
    {
        _redis = redis;
        _keys = keys;
        _log = log;
    }

    private IDatabase Db => _redis.GetDatabase();

    private static DateTime? ParseIso(RedisValue v) =>
        v.IsNullOrEmpty ? null : (DateTime.TryParse(v!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null);
    private static bool ParseBool(RedisValue v) => !v.IsNullOrEmpty && (v == "1" || string.Equals((string?)v, "true", StringComparison.OrdinalIgnoreCase));
    private static int ParseInt(RedisValue v) => v.IsNullOrEmpty ? 0 : (int.TryParse((string?)v, out var i) ? i : 0);
    private static long ParseLong(RedisValue v) => v.IsNullOrEmpty ? 0 : (long.TryParse((string?)v, out var i) ? i : 0);
    private static RedisValue Get(Dictionary<string, RedisValue> map, string key)
        => map.TryGetValue(key, out var v) ? v : RedisValue.Null;

    public async Task<IReadOnlyCollection<string>> ListQueueNamesAsync(string t, string s, CancellationToken ct)
    {
        var rv = await Db.SetMembersAsync(_keys.Queues(t, s));
        return rv.Select(v => (string)v!).Where(x => !string.IsNullOrEmpty(x)).ToArray();
    }

    public async Task<QueueLiveStateDto?> GetQueueAsync(string t, string s, string q, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.Queue(t, s, q));
        if (h.Length == 0) return null;
        var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return new QueueLiveStateDto
        {
            TenantId = t,
            ServerId = s,
            QueueName = (string?)Get(m, "queueName") ?? q,
            Strategy = (string?)Get(m, "strategy"),
            MaxLength = ParseInt(Get(m, "maxLength")),
            WaitingCount = ParseInt(Get(m, "waitingCount")),
            MemberCount = ParseInt(Get(m, "memberCount")),
            AvailableAgentCount = ParseInt(Get(m, "availableAgentCount")),
            RingingAgentCount = ParseInt(Get(m, "ringingAgentCount")),
            TalkingAgentCount = ParseInt(Get(m, "talkingAgentCount")),
            PausedAgentCount = ParseInt(Get(m, "pausedAgentCount")),
            UnavailableAgentCount = ParseInt(Get(m, "unavailableAgentCount")),
            Completed = ParseLong(Get(m, "completed")),
            Abandoned = ParseLong(Get(m, "abandoned")),
            LastSnapshotUtc = ParseIso(Get(m, "lastSnapshotUtc")),
            LastEventUtc = ParseIso(Get(m, "lastEventUtc")) ?? DateTime.UtcNow,
        };
    }

    public async Task<IReadOnlyCollection<QueueAgentLiveStateDto>> GetQueueAgentsAsync(string t, string s, string q, CancellationToken ct)
    {
        var ids = await Db.SetMembersAsync(_keys.QueueMembers(t, s, q));
        var list = new List<QueueAgentLiveStateDto>(ids.Length);
        foreach (var idv in ids)
        {
            var id = (string?)idv;
            if (string.IsNullOrEmpty(id)) continue;
            var qa = await Db.HashGetAllAsync(_keys.QueueAgent(t, s, q, id));
            var ga = await Db.HashGetAllAsync(_keys.Agent(t, s, id));
            if (qa.Length == 0 && ga.Length == 0) continue;
            var merged = ga.Concat(qa).ToDictionary(e => (string)e.Name!, e => e.Value);
            list.Add(BuildAgent(t, s, id, q, merged));
        }
        return list;
    }

    public async Task<IReadOnlyCollection<QueueWaitingCallerStateDto>> GetWaitingCallersAsync(string t, string s, string q, CancellationToken ct)
    {
        var ids = await Db.SortedSetRangeByScoreAsync(_keys.QueueWaiting(t, s, q));
        var list = new List<QueueWaitingCallerStateDto>(ids.Length);
        foreach (var idv in ids)
        {
            var id = (string?)idv;
            if (string.IsNullOrEmpty(id)) continue;
            var h = await Db.HashGetAllAsync(_keys.QueueCall(t, s, id));
            if (h.Length == 0) continue;
            var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
            list.Add(new QueueWaitingCallerStateDto
            {
                TenantId = t,
                ServerId = s,
                CallId = id,
                UniqueId = (string?)Get(m, "uniqueId") ?? id,
                LinkedId = (string?)Get(m, "linkedId"),
                QueueName = (string?)Get(m, "queueName") ?? q,
                Channel = (string?)Get(m, "channel"),
                CallerIdNumber = (string?)Get(m, "callerIdNumber"),
                CallerIdName = (string?)Get(m, "callerIdName"),
                Position = ParseInt(Get(m, "position")),
                OriginalPosition = m.ContainsKey("originalPosition") ? ParseInt(Get(m, "originalPosition")) : null,
                Status = (string?)Get(m, "status") ?? "Waiting",
                JoinedAtUtc = ParseIso(Get(m, "joinedAtUtc")) ?? DateTime.UtcNow,
                ConnectedAtUtc = ParseIso(Get(m, "connectedAtUtc")),
                LeftAtUtc = ParseIso(Get(m, "leftAtUtc")),
                AbandonedAtUtc = ParseIso(Get(m, "abandonedAtUtc")),
                AgentId = (string?)Get(m, "agentId"),
                AgentInterface = (string?)Get(m, "agentInterface"),
                HoldTimeSeconds = m.ContainsKey("holdTimeSeconds") && !Get(m, "holdTimeSeconds").IsNullOrEmpty ? ParseInt(Get(m, "holdTimeSeconds")) : null,
                LeaveReason = (string?)Get(m, "leaveReason"),
                LastEventUtc = ParseIso(Get(m, "lastEventUtc")) ?? DateTime.UtcNow,
            });
        }
        return list;
    }

    public async Task<IReadOnlyCollection<string>> ListAgentIdsAsync(string t, string s, CancellationToken ct)
    {
        var rv = await Db.SetMembersAsync(_keys.Agents(t, s));
        return rv.Select(v => (string)v!).Where(x => !string.IsNullOrEmpty(x)).ToArray();
    }

    public async Task<QueueAgentLiveStateDto?> GetAgentAsync(string t, string s, string agentId, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.Agent(t, s, agentId));
        if (h.Length == 0) return null;
        var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return BuildAgent(t, s, agentId, (string?)Get(m, "activeQueue"), m);
    }

    public async Task<AmiServerStatusDto?> GetAmiStatusAsync(string t, string s, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.AmiStatus(t, s));
        if (h.Length == 0) return null;
        var map = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return new AmiServerStatusDto
        {
            TenantId = t,
            ServerId = s,
            Connected = ParseBool(Get(map, "connected")),
            ConnectionStatus = (string?)Get(map, "connectionStatus") ?? "Unknown",
            LastConnectedUtc = ParseIso(Get(map, "lastConnectedUtc")),
            LastDisconnectedUtc = ParseIso(Get(map, "lastDisconnectedUtc")),
            LastEventUtc = ParseIso(Get(map, "lastEventUtc")),
            LastSnapshotUtc = ParseIso(Get(map, "lastSnapshotUtc")),
            SnapshotStatus = (string?)Get(map, "snapshotStatus") ?? "Unknown",
            IsStateStale = ParseBool(Get(map, "isStateStale")),
            LastError = (string?)Get(map, "lastError"),
        };
    }

    private static QueueAgentLiveStateDto BuildAgent(string t, string s, string agentId, string? q, Dictionary<string, RedisValue> m)
    {
        return new QueueAgentLiveStateDto
        {
            TenantId = t,
            ServerId = s,
            QueueName = q,
            AgentId = agentId,
            Interface = (string?)Get(m, "interface") ?? agentId,
            StateInterface = (string?)Get(m, "stateInterface"),
            MemberName = (string?)Get(m, "memberName"),
            StatusCode = ParseInt(Get(m, "statusCode")),
            Status = (string?)Get(m, "status") ?? "Unknown",
            Paused = ParseBool(Get(m, "paused")),
            PausedReason = (string?)Get(m, "pausedReason"),
            InCall = ParseBool(Get(m, "inCall")),
            RingInUse = ParseBool(Get(m, "ringInUse")),
            Penalty = ParseInt(Get(m, "penalty")),
            CallsTaken = ParseInt(Get(m, "callsTaken")),
            ActiveCallId = (string?)Get(m, "activeCallId"),
            ActiveLinkedId = (string?)Get(m, "activeLinkedId"),
            ActiveChannel = (string?)Get(m, "activeChannel"),
            RingingSinceUtc = ParseIso(Get(m, "ringingSinceUtc")),
            ConnectedAtUtc = ParseIso(Get(m, "connectedAtUtc")),
            WrapUpUntilUtc = ParseIso(Get(m, "wrapUpUntilUtc")),
            LastCallUtc = ParseIso(Get(m, "lastCallUtc")),
            LastEventUtc = ParseIso(Get(m, "lastEventUtc")) ?? DateTime.UtcNow,
        };
    }
}
