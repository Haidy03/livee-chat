using System.Text.Json;
using HelperLib.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VoiceFlow.Core.Helpers;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Core.Models;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Reports.Infrastructure.Telemetry;

/// <summary>
/// Redis-backed implementation of <see cref="ILiveCallRegistry"/>.
///
/// Key layout (prefix = options.KeyPrefix, e.g. "vf:um"):
///   {p}:{t}:call:{callId}              String  JSON LiveCall (TTL = CallTtl)
///   {p}:{t}:active                     Set     active callIds
///   {p}:{t}:state:{state}              Set     callIds per coarse state
///   {p}:{t}:node:{flowId}:{nodeKey}    Set     callIds per flow node
///   {p}:{t}:queue                      ZSet    score = enteredQueueAt epoch ms
///   {p}:{t}:metrics                    Hash    avgHandleSec / slaPercent / slaTarget
///   {p}:{t}:abandoned:{yyyymmdd}       String  INCR on /end abandoned
///   {p}:{t}:locations:{callId}         Hash   {state, flowNode} previously held — to clean up sets on transition
/// </summary>
public sealed class RedisLiveCallRegistry : ILiveCallRegistry
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RedisSentinelConnectionFactory _redisSentinelConnectionFactory;
    private readonly CallTTlOptions _options;
    private readonly ILogger<RedisLiveCallRegistry> _log;
    private readonly IAccountRepository _accountRepository;

    public RedisLiveCallRegistry(RedisSentinelConnectionFactory redisSentinelConnectionFactory,
        IOptions<CallTTlOptions> options, ILogger<RedisLiveCallRegistry> log , IAccountRepository accountRepository)
    {
        _redisSentinelConnectionFactory = redisSentinelConnectionFactory;
        _options = options.Value;
        _log = log;
        _accountRepository = accountRepository;
    }

    private IDatabase Db => _redisSentinelConnectionFactory.GetDatabase();
    private string T(string tenantId) => $"{_options.KeyPrefix}:{Sanitize(tenantId)}";
    private static string Sanitize(string s) => string.IsNullOrWhiteSpace(s) ? "default" : s;

    public async Task RecordStateAsync(LiveCallRecord call, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(call.CallId)) throw new ArgumentException("callId required", nameof(call));
        var t = T(call.TenantId);
        var db = Db;

        // Read previous location so we can pull the call out of stale sets.
        var locKey = $"{t}:locations:{call.CallId}";
        var prevState = (string?)await db.HashGetAsync(locKey, "state");
        var prevNode = (string?)await db.HashGetAsync(locKey, "node");

        var batch = db.CreateBatch();
        var tasks = new List<Task>();

        var stateWire = call.State.ToWire();


        var account = await _accountRepository.GetByIdAsync(call.TenantId, ct);
        var ivrTimeOut = TimeSpan.FromMinutes(account?.IvrTimeout ?? 30);

        // Move out of prior sets.
        if (!string.IsNullOrEmpty(prevState) && prevState != stateWire)
            tasks.Add(batch.SetRemoveAsync($"{t}:state:{prevState}", call.CallId));
        if (!string.IsNullOrEmpty(prevNode))
        {
            var newNode = !string.IsNullOrEmpty(call.FlowId) && !string.IsNullOrEmpty(call.NodeKey)
                ? $"{call.FlowId}:{call.NodeKey}" : null;
            if (prevNode != newNode)
                tasks.Add(batch.SetRemoveAsync($"{t}:node:{prevNode}", call.CallId));
        }
        if (prevState == "queue" && !call.State.IsQueue())
            tasks.Add(batch.SortedSetRemoveAsync($"{t}:queue", call.CallId));

        // Active set + call JSON.
        tasks.Add(batch.SetAddAsync($"{t}:active", call.CallId));
        var json = JsonSerializer.Serialize(call, Json);
        tasks.Add(batch.StringSetAsync($"{t}:call:{call.CallId}", json, ivrTimeOut));

        // New state / node membership.
        tasks.Add(batch.SetAddAsync($"{t}:state:{stateWire}", call.CallId));
        string? newNodeKey = null;
        if (!string.IsNullOrEmpty(call.FlowId) && !string.IsNullOrEmpty(call.NodeKey))
        {
            newNodeKey = $"{call.FlowId}:{call.NodeKey}";
            tasks.Add(batch.SetAddAsync($"{t}:node:{newNodeKey}", call.CallId));
        }

        if (call.State.IsQueue())
        {
            var enteredMs = DateTimeOffset.TryParse(call.EnteredStateAt, out var d)
                ? d.ToUnixTimeMilliseconds() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tasks.Add(batch.SortedSetAddAsync($"{t}:queue", call.CallId, enteredMs));
        }

        // Update locations hash.
        var locFields = new List<HashEntry> { new("state", stateWire) };
        if (newNodeKey != null) locFields.Add(new("node", newNodeKey));
        tasks.Add(batch.HashSetAsync(locKey, locFields.ToArray()));
        tasks.Add(batch.KeyExpireAsync(locKey, ivrTimeOut));

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task RemoveAsync(string tenantId, string callId, string reason, CancellationToken ct)
    {
        var t = T(tenantId);
        var db = Db;
        var locKey = $"{t}:locations:{callId}";
        var prevState = (string?)await db.HashGetAsync(locKey, "state");
        var prevNode = (string?)await db.HashGetAsync(locKey, "node");

        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.SetRemoveAsync($"{t}:active", callId),
            batch.KeyDeleteAsync($"{t}:call:{callId}"),
            batch.KeyDeleteAsync(locKey),
            batch.SortedSetRemoveAsync($"{t}:queue", callId),
        };
        if (!string.IsNullOrEmpty(prevState))
            tasks.Add(batch.SetRemoveAsync($"{t}:state:{prevState}", callId));
        if (!string.IsNullOrEmpty(prevNode))
            tasks.Add(batch.SetRemoveAsync($"{t}:node:{prevNode}", callId));

        if (string.Equals(reason, "abandoned", StringComparison.OrdinalIgnoreCase))
        {
            var day = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
            tasks.Add(batch.StringIncrementAsync($"{t}:abandoned:{day}"));
            tasks.Add(batch.KeyExpireAsync($"{t}:abandoned:{day}", TimeSpan.FromDays(2)));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpdateMetricsAsync(string tenantId, int? avgHandleSec, int? slaPercent, int? slaTargetPercent, CancellationToken ct)
    {
        var t = T(tenantId);
        var entries = new List<HashEntry>();
        if (avgHandleSec.HasValue) entries.Add(new("avgHandleSec", avgHandleSec.Value));
        if (slaPercent.HasValue) entries.Add(new("slaPercent", slaPercent.Value));
        if (slaTargetPercent.HasValue) entries.Add(new("slaTargetPercent", slaTargetPercent.Value));
        if (entries.Count == 0) return;
        await Db.HashSetAsync($"{t}:metrics", entries.ToArray());
    }

    public async Task<LiveCallsSnapshot> GetSnapshotAsync(string tenantId, CancellationToken ct)
    {
        var t = T(tenantId);
        var db = Db;
        var activeIds = await db.SetMembersAsync($"{t}:active");

        var calls = new List<LiveCallRecord>(activeIds.Length);
        if (activeIds.Length > 0)
        {
            var keys = activeIds.Select(id => (RedisKey)$"{t}:call:{id}").ToArray();
            var values = await db.StringGetAsync(keys);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].IsNullOrEmpty) continue;
                try
                {
                    var call = JsonSerializer.Deserialize<LiveCallRecord>(values[i]!, Json);
                    if (call != null) calls.Add(call);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to parse LiveCall for {Key}", keys[i]);
                }
            }
        }

        // Queue ordering.
        var queueEntries = await db.SortedSetRangeByScoreWithScoresAsync($"{t}:queue");
        var queueOrder = queueEntries.Select(e => (string)e.Element!).ToList();
        int longestWait = 0;
        if (queueEntries.Length > 0)
        {
            var head = (long)queueEntries[0].Score;
            longestWait = (int)Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - head) / 1000);
        }

        // State counts — one set per CallState wire label, plus aggregated coarse buckets for back-compat.
        var stateNames = CallStateExtensions.All.Select(s => s.ToWire()).ToArray();
        var stateTasks = stateNames.Select(s => db.SetLengthAsync($"{t}:state:{s}")).ToArray();
        await Task.WhenAll(stateTasks);
        var stateCounts = new Dictionary<string, int>();
        for (int i = 0; i < stateNames.Length; i++)
            stateCounts[stateNames[i]] = (int)stateTasks[i].Result;
        // Coarse buckets used by the Users Map UI.
        foreach (var bucket in new[] { "ivr", "ai", "agent", "queue", "vm", "survey" })
            stateCounts[bucket] = 0;
        foreach (var s in CallStateExtensions.All)
        {
            var n = stateCounts[s.ToWire()];
            if (n == 0) continue;
            var bucket = s.ToCoarseBucket();
            if (stateCounts.ContainsKey(bucket)) stateCounts[bucket] += n;
        }

        // Node counts — derived from active calls' current node, plus any node-specific set we want to back-fill.
        var nodeCounts = new Dictionary<string, int>();
        foreach (var c in calls)
        {
            if (string.IsNullOrEmpty(c.NodeKey)) continue;
            var key = string.IsNullOrEmpty(c.FlowId) ? c.NodeKey! : $"{c.FlowId}:{c.NodeKey}";
            nodeCounts[key] = nodeCounts.GetValueOrDefault(key) + 1;
            // Also index by bare nodeKey for FlowSummary lookups that don't know the flowId.
            if (!string.IsNullOrEmpty(c.FlowId))
                nodeCounts[c.NodeKey!] = nodeCounts.GetValueOrDefault(c.NodeKey!) + 1;
        }

        // Metrics + abandoned counter.
        var metrics = await db.HashGetAllAsync($"{t}:metrics");
        int avg = 0, sla = 0, slaT = 0;
        foreach (var e in metrics)
        {
            switch ((string)e.Name!)
            {
                case "avgHandleSec": int.TryParse(e.Value, out avg); break;
                case "slaPercent": int.TryParse(e.Value, out sla); break;
                case "slaTargetPercent": int.TryParse(e.Value, out slaT); break;
            }
        }
        var day = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var abandoned = (int)(long)await db.StringGetAsync($"{t}:abandoned:{day}");

        return new LiveCallsSnapshot
        {
            Calls = calls,
            StateCounts = stateCounts,
            NodeCounts = nodeCounts,
            QueueOrder = queueOrder,
            LongestWaitSec = longestWait,
            AvgHandleSec = avg,
            SlaPercent = sla,
            SlaTargetPercent = slaT,
            AbandonedToday = abandoned,
        };
    }
}
