using HelperLib.Redis;
using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using StackExchange.Redis;

namespace Outbound.Event.Campaign.RateLimiting;

/// <summary>
/// All-or-nothing multi-level admission check. A single Redis Lua script checks four fixed-window
/// counters (trunk CPS per second; provider/tenant/campaign per minute) and increments each only
/// if all pass. On any denial nothing is consumed — no partial-consume drift.
///
/// <para>Fixed-window buckets are simpler than sliding-window and correct enough for outbound
/// pacing at these limits; the burst ceiling equals the configured per-window value.</para>
/// </summary>
public sealed class CampaignRateLimiter
{
    // KEYS[1..4] = per-second trunk, per-minute provider, tenant, campaign.
    // ARGV[1..4] = respective limits. ARGV[5]=second bucket ttl, ARGV[6]=minute bucket ttl.
    private const string AdmitLua = @"
local kt = KEYS[1]
local kp = KEYS[2]
local kn = KEYS[3]
local kc = KEYS[4]
local lt = tonumber(ARGV[1])
local lp = tonumber(ARGV[2])
local ln = tonumber(ARGV[3])
local lc = tonumber(ARGV[4])
local tt = tonumber(ARGV[5])
local tm = tonumber(ARGV[6])
local vt = tonumber(redis.call('GET', kt) or '0')
local vp = tonumber(redis.call('GET', kp) or '0')
local vn = tonumber(redis.call('GET', kn) or '0')
local vc = tonumber(redis.call('GET', kc) or '0')
if vt >= lt or vp >= lp or vn >= ln or vc >= lc then return 0 end
redis.call('INCR', kt); redis.call('EXPIRE', kt, tt)
redis.call('INCR', kp); redis.call('EXPIRE', kp, tm)
redis.call('INCR', kn); redis.call('EXPIRE', kn, tm)
redis.call('INCR', kc); redis.call('EXPIRE', kc, tm)
return 1
";

    private readonly RedisSentinelConnectionFactory _redis;
    private readonly CampaignRateLimitOptions _opt;
    private readonly ILogger<CampaignRateLimiter> _log;

    public CampaignRateLimiter(
        RedisSentinelConnectionFactory redis,
        IOptions<CampaignRateLimitOptions> opt,
        ILogger<CampaignRateLimiter> log)
    {
        _redis = redis;
        _opt = opt.Value;
        _log = log;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task<bool> TryAdmitAsync(CampaignModel campaign, CancellationToken ct)
    {
        var channel = "voice";
        var now = DateTimeOffset.UtcNow;
        var secBucket = now.ToUnixTimeSeconds();
        var minBucket = secBucket / 60;

        var keys = new RedisKey[]
        {
            $"outbound:rl:trunk:global:{secBucket}",
            $"outbound:rl:provider:{channel}:{minBucket}",
            $"outbound:rl:tenant:{campaign.TenantId}:{minBucket}",
            $"outbound:rl:campaign:{campaign.Id}:{minBucket}",
        };
        var args = new RedisValue[]
        {
            _opt.TrunkCps,
            _opt.ProviderPerMinute,
            _opt.TenantPerMinute,
            _opt.CampaignPerMinute,
            2,     // second bucket TTL (small overlap safety)
            65,    // minute bucket TTL
        };

        try
        {
            var res = (long)await Db.ScriptEvaluateAsync(AdmitLua, keys, args);
            return res == 1L;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Rate limiter Redis call failed — failing open for this attempt.");
            return true; // Fail-open so a Redis blip doesn't halt all dialing.
        }
    }
}
