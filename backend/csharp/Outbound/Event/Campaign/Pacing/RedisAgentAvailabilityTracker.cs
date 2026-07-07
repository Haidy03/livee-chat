using System.Collections.Concurrent;
using HelperLib.Redis;
using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using StackExchange.Redis;

namespace Outbound.Event.Campaign.Pacing;

/// <summary>
/// Free-agent tracker that reads the live queue state CTI already publishes. Key layout mirrors
/// CTI's <c>QueueMonitoringKeys</c>:
///   {prefix}:{env}:{tenantId}:{serverId}:queue:{queueName}:members         (SET of agentIds)
///   {prefix}:{env}:{tenantId}:{serverId}:queue:{queueName}:agent:{id}       (HASH: status/inCall/paused/statusCode)
/// The lookup takes a resolved queue name so it works for both assignedMode values.
/// </summary>
public sealed class RedisAgentAvailabilityTracker : IAgentAvailabilityTracker
{
    private readonly RedisSentinelConnectionFactory _redis;
    private readonly RedisKeyspaceOptions _keys;
    private readonly ILogger<RedisAgentAvailabilityTracker> _log;
    private readonly ConcurrentDictionary<string, (DateTime exp, int value)> _cache = new();
    private readonly TimeSpan _cacheTtl;

    public RedisAgentAvailabilityTracker(
        RedisSentinelConnectionFactory redis,
        IOptions<RedisKeyspaceOptions> opt,
        ILogger<RedisAgentAvailabilityTracker> log)
    {
        _redis = redis;
        _keys = opt.Value;
        _log = log;
        _cacheTtl = TimeSpan.FromMilliseconds(Math.Max(100, _keys.FreeAgentCacheMilliseconds));
    }

    private IDatabase Db => _redis.GetDatabase();

    private string Base(string tenantId, string serverId) =>
        $"{_keys.RedisKeyPrefix}:{_keys.Environment}:{tenantId}:{serverId}";

    private string MembersKey(string tenantId, string serverId, string queueName) =>
        $"{Base(tenantId, serverId)}:queue:{queueName}:members";

    private string AgentKey(string tenantId, string serverId, string queueName, string agentId) =>
        $"{Base(tenantId, serverId)}:queue:{queueName}:agent:{agentId}";

    private string AmiStatusKey(string tenantId, string serverId) =>
        $"{Base(tenantId, serverId)}:ami-status";

    /// <summary>Kept for interface compatibility. Not used by the new dispatcher (which passes
    /// the resolved queue via <see cref="GetFreeAgentsForQueueAsync"/>).</summary>
    public int GetFreeAgents(string campaignId) => 0;

    public async Task<int> GetFreeAgentsForCampaignAsync(CampaignModel campaign, CancellationToken ct)
    {
        var queueName = QueueNameResolver.Resolve(campaign);
        if (string.IsNullOrWhiteSpace(queueName)) return 0;
        return await GetFreeAgentsForQueueAsync(campaign.TenantId, queueName!, ct);
    }

    public async Task<int> GetFreeAgentsForQueueAsync(string tenantId, string queueName, CancellationToken ct)
    {
        var cacheKey = $"{tenantId}|{queueName}";
        if (_cache.TryGetValue(cacheKey, out var hit) && hit.exp > DateTime.UtcNow)
            return hit.value;

        try
        {
            var serverId = _keys.DefaultServerId;

            // Primary tenant for the key prefix: an explicit override if configured, else the
            // campaign's real tenant (the correct post-fix behavior).
            var primaryTenant = string.IsNullOrWhiteSpace(_keys.TenantOverride) ? tenantId : _keys.TenantOverride;

            // Staleness guard.
            // if (await IsAmiStaleAsync(primaryTenant, serverId))
            //     return CacheAndReturn(cacheKey, 0);
            
            var free = await CountFreeForTenantAsync(primaryTenant, serverId, queueName, ct);

            // Fallback: while a campaign (__qc_) queue is still bucketed under "default" (e.g. CTI not
            // yet redeployed with the __qc_ parser fix), the real-tenant key won't exist — retry there.
            if (free is null && !string.Equals(primaryTenant, "default", StringComparison.Ordinal))
                free = await CountFreeForTenantAsync("default", serverId, queueName, ct);

            return CacheAndReturn(cacheKey, free ?? 0);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "RedisAgentAvailabilityTracker read failed for {Tenant}/{Queue}", tenantId, queueName);
            return 0;
        }
    }

    /// <summary>
    /// Counts free agents for <paramref name="queueName"/> under a specific tenant key prefix.
    /// Returns <c>null</c> when the queue's members set does not exist under that tenant (so the
    /// caller can try a fallback tenant), or the free-agent count (possibly 0) when it does.
    /// </summary>
    private async Task<int?> CountFreeForTenantAsync(string keyTenant, string serverId, string queueName, CancellationToken ct)
    {
        var members = await Db.SetMembersAsync(MembersKey(keyTenant, serverId, queueName));
        if (members.Length == 0) return null;

        var batch = Db.CreateBatch();
        var tasks = new Task<HashEntry[]>[members.Length];
        for (var i = 0; i < members.Length; i++)
        {
            var agentId = (string?)members[i] ?? string.Empty;
            tasks[i] = batch.HashGetAllAsync(AgentKey(keyTenant, serverId, queueName, agentId));
        }
        batch.Execute();
        await Task.WhenAll(tasks);

        var free = 0;
        foreach (var t in tasks)
        {
            var entries = t.Result;
            if (entries.Length == 0) continue;
            if (IsFree(entries)) free++;
        }
        return free;
    }

    private static bool IsFree(HashEntry[] entries)
    {
        string? status = null; string? paused = null; string? inCall = null; string? statusCode = null;
        foreach (var e in entries)
        {
            var name = (string?)e.Name;
            var v = (string?)e.Value;
            if (name is null) continue;
            switch (name)
            {
                case "status": status = v; break;
                case "paused": paused = v; break;
                case "inCall": inCall = v; break;
                case "statusCode": statusCode = v; break;
            }
        }

        if (paused == "1" || string.Equals(paused, "true", StringComparison.OrdinalIgnoreCase)) return false;
        if (inCall == "1" || string.Equals(inCall, "true", StringComparison.OrdinalIgnoreCase)) return false;

        if (!string.IsNullOrWhiteSpace(status))
            return string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase);

        return statusCode == "1";
    }

    private async Task<bool> IsAmiStaleAsync(string tenantId, string serverId)
    {
        try
        {
            // CTI publishes ami-status as a HASH (connected/connectionStatus/lastEventUtc/isStateStale/...).
            var fields = await Db.HashGetAsync(AmiStatusKey(tenantId, serverId), new RedisValue[]
            {
                "connected", "isStateStale", "lastEventUtc", "connectionStatus"
            });
            var connected = (string?)fields[0];
            var isStateStale = (string?)fields[1];
            var lastEventUtc = (string?)fields[2];
            var connectionStatus = (string?)fields[3];

            // No signal at all — don't block.
            if (fields.All(f => f.IsNullOrEmpty)) return false;

            // Explicitly disconnected → stale.
            if (connected == "0" || string.Equals(connectionStatus, "Disconnected", StringComparison.OrdinalIgnoreCase))
                return true;

            // CTI already computes staleness — trust it when present.
            if (isStateStale == "1" || string.Equals(isStateStale, "true", StringComparison.OrdinalIgnoreCase))
                return true;

            // Otherwise fall back to heartbeat age from the last event timestamp.
            if (DateTime.TryParse(lastEventUtc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var last))
            {
                return (DateTime.UtcNow - last).TotalSeconds > _keys.AmiStaleStateSeconds;
            }

            // Connected with no parseable heartbeat — assume fresh.
            return false;
        }
        catch { return false; }
    }

    private int CacheAndReturn(string cacheKey, int value)
    {
        _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheTtl), value);
        return value;
    }
}
