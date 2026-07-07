using System.Globalization;
using CtiBackend.Options;
using CtiBackend.Services.QueueMonitoring.Models;
using HelperLib.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CtiBackend.Services.QueueMonitoring;

public sealed class QueueMonitoringRedisRepository : IQueueMonitoringRedisRepository
{
    private readonly RedisSentinelConnectionFactory _redis;
    private readonly QueueMonitoringKeys _keys;
    private readonly QueueMonitoringOptions _opts;
    private readonly ILogger<QueueMonitoringRedisRepository> _log;

    public QueueMonitoringRedisRepository(
        RedisSentinelConnectionFactory redis,
        QueueMonitoringKeys keys,
        IOptions<QueueMonitoringOptions> opts,
        ILogger<QueueMonitoringRedisRepository> log)
    {
        _redis = redis;
        _keys = keys;
        _opts = opts.Value;
        _log = log;
    }

    private IDatabase Db => _redis.GetDatabase();
    private static string Iso(DateTime? d) => d.HasValue ? d.Value.ToString("O", CultureInfo.InvariantCulture) : "";
    private static DateTime? ParseIso(RedisValue v) =>
        v.IsNullOrEmpty ? null : (DateTime.TryParse(v!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null);
    private static bool ParseBool(RedisValue v) => !v.IsNullOrEmpty && (v == "1" || string.Equals((string?)v, "true", StringComparison.OrdinalIgnoreCase));
    private static int ParseInt(RedisValue v) => v.IsNullOrEmpty ? 0 : (int.TryParse((string?)v, out var i) ? i : 0);
    private static long ParseLong(RedisValue v) => v.IsNullOrEmpty ? 0 : (long.TryParse((string?)v, out var i) ? i : 0);

    public async Task UpsertQueueAsync(QueueLiveState q, CancellationToken ct)
    {
        var db = Db;
        var key = _keys.Queue(q.TenantId, q.ServerId, q.QueueName);
        var entries = new HashEntry[]
        {
            new("queueName", q.QueueName),
            new("tenantId", q.TenantId),
            new("serverId", q.ServerId),
            new("strategy", q.Strategy ?? ""),
            new("maxLength", q.MaxLength),
            new("waitingCount", q.WaitingCount),
            new("memberCount", q.MemberCount),
            new("availableAgentCount", q.AvailableAgentCount),
            new("ringingAgentCount", q.RingingAgentCount),
            new("talkingAgentCount", q.TalkingAgentCount),
            new("pausedAgentCount", q.PausedAgentCount),
            new("unavailableAgentCount", q.UnavailableAgentCount),
            new("completed", q.Completed),
            new("abandoned", q.Abandoned),
            new("lastSnapshotUtc", Iso(q.LastSnapshotUtc)),
            new("lastEventUtc", Iso(q.LastEventUtc == default ? DateTime.UtcNow : q.LastEventUtc)),
        };
        var batch = db.CreateBatch();
        var tasks = new[]
        {
            batch.HashSetAsync(key, entries),
            batch.SetAddAsync(_keys.Queues(q.TenantId, q.ServerId), q.QueueName),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpsertQueueAgentAsync(QueueAgentLiveState a, CancellationToken ct)
    {
        var db = Db;
        var batch = db.CreateBatch();
        var tasks = new List<Task>();

        // Global agent
        var gkey = _keys.Agent(a.TenantId, a.ServerId, a.AgentId);
        tasks.Add(batch.HashSetAsync(gkey, new HashEntry[]
        {
            new("agentId", a.AgentId),
            new("interface", a.Interface),
            new("stateInterface", a.StateInterface ?? ""),
            new("memberName", a.MemberName ?? ""),
            new("statusCode", a.StatusCode),
            new("status", a.Status),
            new("paused", a.Paused ? 1 : 0),
            new("pausedReason", a.PausedReason ?? ""),
            new("inCall", a.InCall ? 1 : 0),
            new("ringInUse", a.RingInUse ? 1 : 0),
            new("activeCallId", a.ActiveCallId ?? ""),
            new("activeLinkedId", a.ActiveLinkedId ?? ""),
            new("activeChannel", a.ActiveChannel ?? ""),
            new("activeQueue", a.QueueName ?? ""),
            new("ringingSinceUtc", Iso(a.RingingSinceUtc)),
            new("connectedAtUtc", Iso(a.ConnectedAtUtc)),
            new("wrapUpUntilUtc", Iso(a.WrapUpUntilUtc)),
            new("lastCallUtc", Iso(a.LastCallUtc)),
            new("callsTaken", a.CallsTaken),
            new("lastEventUtc", Iso(a.LastEventUtc == default ? DateTime.UtcNow : a.LastEventUtc)),
        }));
        tasks.Add(batch.SetAddAsync(_keys.Agents(a.TenantId, a.ServerId), a.AgentId));

        if (!string.IsNullOrEmpty(a.QueueName))
        {
            var qkey = _keys.QueueAgent(a.TenantId, a.ServerId, a.QueueName!, a.AgentId);
            tasks.Add(batch.HashSetAsync(qkey, new HashEntry[]
            {
                new("queueName", a.QueueName!),
                new("agentId", a.AgentId),
                new("interface", a.Interface),
                new("memberName", a.MemberName ?? ""),
                new("penalty", a.Penalty),
                new("paused", a.Paused ? 1 : 0),
                new("pausedReason", a.PausedReason ?? ""),
                new("ringInUse", a.RingInUse ? 1 : 0),
                new("statusCode", a.StatusCode),
                new("status", a.Status),
                new("inCall", a.InCall ? 1 : 0),
                new("callsTaken", a.CallsTaken),
                new("lastCallUtc", Iso(a.LastCallUtc)),
                new("lastEventUtc", Iso(a.LastEventUtc == default ? DateTime.UtcNow : a.LastEventUtc)),
            }));
            tasks.Add(batch.SetAddAsync(_keys.QueueMembers(a.TenantId, a.ServerId, a.QueueName!), a.AgentId));
            tasks.Add(batch.SetAddAsync(_keys.Queues(a.TenantId, a.ServerId), a.QueueName!));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task RemoveQueueAgentAsync(string t, string s, string q, string agentId, CancellationToken ct)
    {
        var db = Db;
        var batch = db.CreateBatch();
        var tasks = new[]
        {
            batch.KeyDeleteAsync(_keys.QueueAgent(t, s, q, agentId)),
            batch.SetRemoveAsync(_keys.QueueMembers(t, s, q), agentId),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task AddWaitingCallerAsync(QueueWaitingCallerState c, CancellationToken ct)
    {
        var db = Db;
        var key = _keys.QueueCall(c.TenantId, c.ServerId, c.CallId);

        // Preserve original JoinedAtUtc on duplicate.
        var existingJoined = await db.HashGetAsync(key, "joinedAtUtc");
        var joinedAt = ParseIso(existingJoined) ?? c.JoinedAtUtc;
        if (joinedAt == default) joinedAt = DateTime.UtcNow;
        var originalPos = await db.HashGetAsync(key, "originalPosition");
        var origPosVal = existingJoined.IsNullOrEmpty ? c.Position : (originalPos.IsNullOrEmpty ? c.Position : ParseInt(originalPos));

        var entries = new HashEntry[]
        {
            new("callId", c.CallId),
            new("uniqueId", c.UniqueId),
            new("linkedId", c.LinkedId ?? ""),
            new("queueName", c.QueueName),
            new("channel", c.Channel ?? ""),
            new("callerIdNumber", c.CallerIdNumber ?? ""),
            new("callerIdName", c.CallerIdName ?? ""),
            new("position", c.Position),
            new("originalPosition", origPosVal),
            new("status", "Waiting"),
            new("joinedAtUtc", Iso(joinedAt)),
            new("lastEventUtc", Iso(DateTime.UtcNow)),
        };

        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.HashSetAsync(key, entries),
            batch.SortedSetAddAsync(_keys.QueueWaiting(c.TenantId, c.ServerId, c.QueueName),
                                    c.CallId, new DateTimeOffset(joinedAt, TimeSpan.Zero).ToUnixTimeMilliseconds()),
            batch.SetAddAsync(_keys.Queues(c.TenantId, c.ServerId), c.QueueName),
        };
        // recalc waiting count
        batch.Execute();
        await Task.WhenAll(tasks);
        await RecalcWaitingCountAsync(db, c.TenantId, c.ServerId, c.QueueName);
    }

    public async Task RemoveWaitingCallerAsync(string t, string s, string q, string callId, string? reason, CancellationToken ct)
    {
        var db = Db;
        var key = _keys.QueueCall(t, s, callId);
        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.SortedSetRemoveAsync(_keys.QueueWaiting(t, s, q), callId),
            batch.HashSetAsync(key, new HashEntry[]
            {
                new("status", "LeftQueue"),
                new("leftAtUtc", Iso(DateTime.UtcNow)),
                new("leaveReason", reason ?? ""),
                new("lastEventUtc", Iso(DateTime.UtcNow)),
            }),
            batch.KeyExpireAsync(key, TimeSpan.FromHours(_opts.CompletedWaitingCallerTtlHours)),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
        await RecalcWaitingCountAsync(db, t, s, q);
    }

    public async Task<bool> MarkCallerAbandonedAsync(string t, string s, string q, string callId, int? position, int? originalPosition, int? holdTime, CancellationToken ct)
    {
        var db = Db;
        var key = _keys.QueueCall(t, s, callId);
        // Atomic: only increment abandoned counter if status was not already Abandoned.
        const string lua = @"
local prev = redis.call('HGET', KEYS[1], 'status')
if prev == 'Abandoned' then return 0 end
redis.call('HSET', KEYS[1],
    'status','Abandoned',
    'abandonedAtUtc', ARGV[1],
    'position', ARGV[2],
    'originalPosition', ARGV[3],
    'holdTimeSeconds', ARGV[4],
    'queueName', ARGV[5],
    'lastEventUtc', ARGV[1])
redis.call('ZREM', KEYS[2], ARGV[6])
redis.call('HINCRBY', KEYS[3], 'abandoned', 1)
return 1
";
        var res = (long)await db.ScriptEvaluateAsync(lua,
            new RedisKey[] { key, _keys.QueueWaiting(t, s, q), _keys.Queue(t, s, q) },
            new RedisValue[]
            {
                Iso(DateTime.UtcNow),
                position?.ToString() ?? "",
                originalPosition?.ToString() ?? "",
                holdTime?.ToString() ?? "",
                q,
                callId,
            });
        await db.KeyExpireAsync(key, TimeSpan.FromHours(_opts.CompletedWaitingCallerTtlHours));
        await RecalcWaitingCountAsync(db, t, s, q);
        return res == 1;
    }

    public async Task MarkCallerConnectedAsync(string t, string s, string q, string callId, string agentId, string agentInterface, string? agentChannel, int? holdTime, CancellationToken ct)
    {
        var db = Db;
        var callKey = _keys.QueueCall(t, s, callId);
        var activeKey = _keys.ActiveCall(t, s, callId);
        var batch = db.CreateBatch();
        var now = Iso(DateTime.UtcNow);
        var tasks = new List<Task>
        {
            batch.SortedSetRemoveAsync(_keys.QueueWaiting(t, s, q), callId),
            batch.HashSetAsync(callKey, new HashEntry[]
            {
                new("status", "Connected"),
                new("connectedAtUtc", now),
                new("agentId", agentId),
                new("agentInterface", agentInterface),
                new("holdTimeSeconds", holdTime?.ToString() ?? ""),
                new("lastEventUtc", now),
            }),
            batch.HashSetAsync(activeKey, new HashEntry[]
            {
                new("callId", callId),
                new("queueName", q),
                new("agentId", agentId),
                new("agentInterface", agentInterface),
                new("agentChannel", agentChannel ?? ""),
                new("status", "Connected"),
                new("connectedAtUtc", now),
                new("holdTimeSeconds", holdTime?.ToString() ?? ""),
                new("lastEventUtc", now),
            }),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
        await RecalcWaitingCountAsync(db, t, s, q);
    }

    public async Task MarkAgentRingingAsync(string t, string s, string q, string agentId, string agentInterface, string? destChannel, string callId, string? linkedId, CancellationToken ct)
    {
        var db = Db;
        var now = Iso(DateTime.UtcNow);
        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.HashSetAsync(_keys.Agent(t, s, agentId), new HashEntry[]
            {
                new("status", "Ringing"),
                new("activeQueue", q),
                new("activeCallId", callId),
                new("activeLinkedId", linkedId ?? ""),
                new("activeChannel", destChannel ?? ""),
                new("ringingSinceUtc", now),
                new("lastEventUtc", now),
            }),
            batch.HashSetAsync(_keys.QueueAgent(t, s, q, agentId), new HashEntry[]
            {
                new("status", "Ringing"),
                new("lastEventUtc", now),
            }),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task MarkAgentConnectedAsync(string t, string s, string q, string agentId, string agentInterface, string? destChannel, string callId, string? linkedId, int? holdTime, CancellationToken ct)
    {
        var db = Db;
        var now = Iso(DateTime.UtcNow);
        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.HashSetAsync(_keys.Agent(t, s, agentId), new HashEntry[]
            {
                new("status", "Talking"),
                new("inCall", 1),
                new("activeQueue", q),
                new("activeCallId", callId),
                new("activeLinkedId", linkedId ?? ""),
                new("activeChannel", destChannel ?? ""),
                new("connectedAtUtc", now),
                new("lastEventUtc", now),
            }),
            batch.HashSetAsync(_keys.QueueAgent(t, s, q, agentId), new HashEntry[]
            {
                new("status", "Talking"),
                new("inCall", 1),
                new("lastEventUtc", now),
            }),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
        await MarkCallerConnectedAsync(t, s, q, callId, agentId, agentInterface, destChannel, holdTime, ct);
    }

    public async Task MarkAgentCompletedAsync(string t, string s, string q, string agentId, string callId, int? talkTime, int? holdTime, string? reason, CancellationToken ct)
    {
        var db = Db;
        var now = Iso(DateTime.UtcNow);
        var nowDt = DateTime.UtcNow;
        var batch = db.CreateBatch();

        // Determine next status from paused flag.
        var pausedRv = await db.HashGetAsync(_keys.Agent(t, s, agentId), "paused");
        var paused = ParseBool(pausedRv);
        var nextStatus = paused ? "Paused" : "Available";

        var tasks = new List<Task>
        {
            batch.HashSetAsync(_keys.Agent(t, s, agentId), new HashEntry[]
            {
                new("status", nextStatus),
                new("inCall", 0),
                new("activeCallId", ""),
                new("activeLinkedId", ""),
                new("activeChannel", ""),
                new("activeQueue", ""),
                new("lastCallUtc", now),
                new("lastEventUtc", now),
            }),
            batch.HashIncrementAsync(_keys.Agent(t, s, agentId), "callsTaken"),
            batch.HashSetAsync(_keys.QueueAgent(t, s, q, agentId), new HashEntry[]
            {
                new("status", nextStatus),
                new("inCall", 0),
                new("lastCallUtc", now),
                new("lastEventUtc", now),
            }),
            batch.HashIncrementAsync(_keys.QueueAgent(t, s, q, agentId), "callsTaken"),
            batch.HashIncrementAsync(_keys.Queue(t, s, q), "completed"),
            batch.HashSetAsync(_keys.ActiveCall(t, s, callId), new HashEntry[]
            {
                new("status", "Completed"),
                new("completedAtUtc", now),
                new("talkTimeSeconds", talkTime?.ToString() ?? ""),
                new("holdTimeSeconds", holdTime?.ToString() ?? ""),
                new("completionReason", reason ?? ""),
                new("lastEventUtc", now),
            }),
            batch.KeyExpireAsync(_keys.ActiveCall(t, s, callId), TimeSpan.FromHours(_opts.CompletedCallTtlHours)),
            batch.HashSetAsync(_keys.QueueCall(t, s, callId), new HashEntry[]
            {
                new("status", "Completed"),
                new("lastEventUtc", now),
            }),
            batch.KeyExpireAsync(_keys.QueueCall(t, s, callId), TimeSpan.FromHours(_opts.CompletedWaitingCallerTtlHours)),
        };
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task<bool> TryAcquireSnapshotLockAsync(string t, string s, TimeSpan ttl, string token, CancellationToken ct)
        => await Db.StringSetAsync(_keys.SnapshotLock(t, s), token, ttl, when: When.NotExists);

    public async Task ReleaseSnapshotLockAsync(string t, string s, string token, CancellationToken ct)
    {
        const string lua = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
        try { await Db.ScriptEvaluateAsync(lua, new RedisKey[] { _keys.SnapshotLock(t, s) }, new RedisValue[] { token }); }
        catch (Exception ex) { _log.LogDebug(ex, "Snapshot lock release failed"); }
    }

    public async Task ApplySnapshotAsync(QueueSnapshotContext ctx, CancellationToken ct)
    {
        var db = Db;
        // Replace queues set with snapshot queues; preserve those still present.
        var snapshotQueues = ctx.Queues.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await db.SetMembersAsync(_keys.Queues(ctx.TenantId, ctx.ServerId));
        foreach (var rv in existing)
        {
            var name = (string?)rv;
            if (!string.IsNullOrEmpty(name) && !snapshotQueues.Contains(name))
            {
                await db.SetRemoveAsync(_keys.Queues(ctx.TenantId, ctx.ServerId), name);
            }
        }

        foreach (var q in ctx.Queues.Values)
        {
            q.LastSnapshotUtc = DateTime.UtcNow;
            // Only overwrite if no newer live event has occurred during snapshot.
            var existingEvt = ParseIso(await db.HashGetAsync(_keys.Queue(ctx.TenantId, ctx.ServerId, q.QueueName), "lastEventUtc"));
            if (existingEvt.HasValue && existingEvt > ctx.StartedAtUtc)
            {
                // Only update snapshot timestamp.
                await db.HashSetAsync(_keys.Queue(ctx.TenantId, ctx.ServerId, q.QueueName),
                    new HashEntry[] { new("lastSnapshotUtc", Iso(DateTime.UtcNow)) });
            }
            else
            {
                await UpsertQueueAsync(q, ct);
            }
        }

        // Replace queue members from snapshot.
        var byQueue = ctx.Members.GroupBy(m => m.QueueName ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var grp in byQueue)
        {
            if (string.IsNullOrEmpty(grp.Key)) continue;
            var memSet = _keys.QueueMembers(ctx.TenantId, ctx.ServerId, grp.Key);
            var snapshotIds = grp.Select(g => g.AgentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var current = await db.SetMembersAsync(memSet);
            foreach (var rv in current)
            {
                var aid = (string?)rv;
                if (!string.IsNullOrEmpty(aid) && !snapshotIds.Contains(aid))
                    await RemoveQueueAgentAsync(ctx.TenantId, ctx.ServerId, grp.Key, aid, ct);
            }
            foreach (var a in grp)
                await UpsertQueueAgentAsync(a, ct);
        }

        // Reconcile waiting callers per queue.
        var waitingByQueue = ctx.WaitingCallers.GroupBy(w => w.QueueName, StringComparer.OrdinalIgnoreCase);
        foreach (var grp in waitingByQueue)
        {
            var zset = _keys.QueueWaiting(ctx.TenantId, ctx.ServerId, grp.Key);
            var snapshotIds = grp.Select(g => g.CallId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var current = await db.SortedSetRangeByScoreAsync(zset);
            foreach (var rv in current)
            {
                var cid = (string?)rv;
                if (!string.IsNullOrEmpty(cid) && !snapshotIds.Contains(cid))
                    await db.SortedSetRemoveAsync(zset, cid);
            }
            foreach (var w in grp)
                await AddWaitingCallerAsync(w, ct);
        }
    }

    public async Task UpdateAmiStatusAsync(AmiServerStatus s, CancellationToken ct)
    {
        await Db.HashSetAsync(_keys.AmiStatus(s.TenantId, s.ServerId), new HashEntry[]
        {
            new("connected", s.Connected ? 1 : 0),
            new("connectionStatus", s.ConnectionStatus),
            new("lastConnectedUtc", Iso(s.LastConnectedUtc)),
            new("lastDisconnectedUtc", Iso(s.LastDisconnectedUtc)),
            new("lastEventUtc", Iso(s.LastEventUtc)),
            new("lastSnapshotUtc", Iso(s.LastSnapshotUtc)),
            new("snapshotStatus", s.SnapshotStatus),
            new("isStateStale", s.IsStateStale ? 1 : 0),
            new("lastError", s.LastError ?? ""),
        });
    }

    public async Task<AmiServerStatus?> GetAmiStatusAsync(string t, string s, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.AmiStatus(t, s));
        if (h.Length == 0) return null;
        var map = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return new AmiServerStatus
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

    public async Task<bool> TrySetDedupAsync(string t, string s, string hash, TimeSpan ttl, CancellationToken ct)
        => await Db.StringSetAsync(_keys.EventDedup(t, s, hash), "1", ttl, when: When.NotExists);

    // ----- Queries -----
    public async Task<IReadOnlyCollection<string>> ListQueueNamesAsync(string t, string s, CancellationToken ct)
    {
        var rv = await Db.SetMembersAsync(_keys.Queues(t, s));
        return rv.Select(v => (string)v!).Where(x => !string.IsNullOrEmpty(x)).ToArray();
    }

    public async Task<QueueLiveState?> GetQueueAsync(string t, string s, string q, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.Queue(t, s, q));
        if (h.Length == 0) return null;
        var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return new QueueLiveState
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

    public async Task<IReadOnlyCollection<QueueAgentLiveState>> GetQueueAgentsAsync(string t, string s, string q, CancellationToken ct)
    {
        var ids = await Db.SetMembersAsync(_keys.QueueMembers(t, s, q));
        var list = new List<QueueAgentLiveState>(ids.Length);
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

    public async Task<IReadOnlyCollection<QueueWaitingCallerState>> GetWaitingCallersAsync(string t, string s, string q, CancellationToken ct)
    {
        var ids = await Db.SortedSetRangeByScoreAsync(_keys.QueueWaiting(t, s, q));
        var list = new List<QueueWaitingCallerState>(ids.Length);
        foreach (var idv in ids)
        {
            var id = (string?)idv;
            if (string.IsNullOrEmpty(id)) continue;
            var h = await Db.HashGetAllAsync(_keys.QueueCall(t, s, id));
            if (h.Length == 0) continue;
            var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
            list.Add(new QueueWaitingCallerState
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

    public async Task<QueueAgentLiveState?> GetAgentAsync(string t, string s, string agentId, CancellationToken ct)
    {
        var h = await Db.HashGetAllAsync(_keys.Agent(t, s, agentId));
        if (h.Length == 0) return null;
        var m = h.ToDictionary(e => (string)e.Name!, e => e.Value);
        return BuildAgent(t, s, agentId, (string?)Get(m, "activeQueue"), m);
    }

    private static QueueAgentLiveState BuildAgent(string t, string s, string agentId, string? q, Dictionary<string, RedisValue> m)
    {
        return new QueueAgentLiveState
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

    private static RedisValue Get(Dictionary<string, RedisValue> map, string key)
        => map.TryGetValue(key, out var v) ? v : RedisValue.Null;

    private async Task RecalcWaitingCountAsync(IDatabase db, string t, string s, string q)
    {
        var count = await db.SortedSetLengthAsync(_keys.QueueWaiting(t, s, q));
        await db.HashSetAsync(_keys.Queue(t, s, q), new HashEntry[]
        {
            new("waitingCount", count),
            new("lastEventUtc", Iso(DateTime.UtcNow)),
        });
        await db.SetAddAsync(_keys.Queues(t, s), q);
    }
}
