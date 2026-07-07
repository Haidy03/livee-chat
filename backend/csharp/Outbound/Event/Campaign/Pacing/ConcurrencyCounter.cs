using HelperLib.Redis;
using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Options;
using StackExchange.Redis;

namespace Outbound.Event.Campaign.Pacing;

/// <summary>
/// Per-campaign in-flight token counter, backed by Redis. This is the dimension the old
/// push-based design lacked — it caps how many originates a campaign has out at any moment,
/// independent of the rate limiter (which caps *speed*).
///
/// <para>
/// <see cref="TryTakeAsync"/> is an atomic check-and-incr Lua: it increments the counter and
/// refreshes its TTL only if the resulting value would not exceed <paramref name="maxAllowed"/>.
/// A leaked increment (crash after take but before <see cref="GiveBackAsync"/>) self-heals via
/// the TTL backstop, and the reaper reconciles the count against the actual number of
/// <c>dialing</c> rows periodically.
/// </para>
/// </summary>
public interface IConcurrencyCounter
{
    Task<bool> TryTakeAsync(string campaignId, int maxAllowed, CancellationToken ct);
    Task GiveBackAsync(string campaignId, CancellationToken ct);
    Task<long> GetCountAsync(string campaignId, CancellationToken ct);
    Task SetCountAsync(string campaignId, long value, CancellationToken ct);
}

public sealed class ConcurrencyCounter : IConcurrencyCounter
{
    // KEYS[1] = counter key, ARGV[1] = max, ARGV[2] = ttl seconds.
    // Returns 1 when the token was taken, 0 when denied.
    private const string TakeLua =
        "local v = tonumber(redis.call('GET', KEYS[1]) or '0') " +
        "if v >= tonumber(ARGV[1]) then return 0 end " +
        "local n = redis.call('INCR', KEYS[1]) " +
        "redis.call('EXPIRE', KEYS[1], tonumber(ARGV[2])) " +
        "return 1";

    // Never let the counter go negative on a stray DECR.
    private const string GiveBackLua =
        "local v = tonumber(redis.call('GET', KEYS[1]) or '0') " +
        "if v <= 0 then return 0 end " +
        "redis.call('DECR', KEYS[1]) " +
        "redis.call('EXPIRE', KEYS[1], tonumber(ARGV[1])) " +
        "return 1";

    private readonly RedisSentinelConnectionFactory _redis;
    private readonly ConcurrencyOptions _opt;
    private readonly ILogger<ConcurrencyCounter> _log;

    public ConcurrencyCounter(
        RedisSentinelConnectionFactory redis,
        IOptions<ConcurrencyOptions> opt,
        ILogger<ConcurrencyCounter> log)
    {
        _redis = redis;
        _opt = opt.Value;
        _log = log;
    }

    private IDatabase Db => _redis.GetDatabase();
    private string Key(string campaignId) => _opt.KeyPrefix + campaignId;

    public async Task<bool> TryTakeAsync(string campaignId, int maxAllowed, CancellationToken ct)
    {
        if (maxAllowed <= 0) return false;
        try
        {
            var res = (long)await Db.ScriptEvaluateAsync(
                TakeLua,
                new RedisKey[] { Key(campaignId) },
                new RedisValue[] { maxAllowed, _opt.TtlSeconds });
            return res == 1L;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ConcurrencyCounter.TryTake failed for {CampaignId}", campaignId);
            return false;
        }
    }

    public async Task GiveBackAsync(string campaignId, CancellationToken ct)
    {
        try
        {
            await Db.ScriptEvaluateAsync(
                GiveBackLua,
                new RedisKey[] { Key(campaignId) },
                new RedisValue[] { _opt.TtlSeconds });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "ConcurrencyCounter.GiveBack failed for {CampaignId}", campaignId);
        }
    }

    public async Task<long> GetCountAsync(string campaignId, CancellationToken ct)
    {
        try
        {
            var v = await Db.StringGetAsync(Key(campaignId));
            return v.IsNullOrEmpty ? 0L : (long.TryParse((string?)v, out var n) ? n : 0L);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "ConcurrencyCounter.GetCount failed for {CampaignId}", campaignId);
            return 0L;
        }
    }

    public async Task SetCountAsync(string campaignId, long value, CancellationToken ct)
    {
        try
        {
            await Db.StringSetAsync(Key(campaignId), value, TimeSpan.FromSeconds(_opt.TtlSeconds));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "ConcurrencyCounter.SetCount failed for {CampaignId}", campaignId);
        }
    }
}
