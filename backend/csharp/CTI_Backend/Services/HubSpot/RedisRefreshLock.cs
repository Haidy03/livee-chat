using HelperLib.Redis;
using StackExchange.Redis;

namespace CtiBackend.Services.HubSpot;

public interface IRefreshLock
{
    /// <summary>
    /// Acquire a per-tenant refresh lock. Returns null if the lock could
    /// not be acquired within <paramref name="wait"/>. The returned
    /// IAsyncDisposable releases the lock.
    /// </summary>
    Task<IAsyncDisposable?> AcquireAsync(string tenantId, TimeSpan ttl, TimeSpan wait, CancellationToken ct);
}

public sealed class RedisRefreshLock : IRefreshLock
{
    private readonly RedisSentinelConnectionFactory _redisSentinelConnectionFactory;
    private IDatabase Db => _redisSentinelConnectionFactory.GetDatabase();
    public RedisRefreshLock(RedisSentinelConnectionFactory redisSentinelConnectionFactory) => _redisSentinelConnectionFactory = redisSentinelConnectionFactory;

    public async Task<IAsyncDisposable?> AcquireAsync(string tenantId, TimeSpan ttl, TimeSpan wait, CancellationToken ct)
    {
        var db = Db;
        var key = $"vf:hubspot:refresh:{tenantId}";
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + wait;
        while (true)
        {
            if (await db.StringSetAsync(key, token, ttl, when: When.NotExists))
                return new Releaser(db, key, token);
            if (DateTime.UtcNow >= deadline) return null;
            await Task.Delay(100, ct);
        }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;
        public Releaser(IDatabase db, string key, string token) { _db = db; _key = key; _token = token; }
        public async ValueTask DisposeAsync()
        {
            // Best-effort compare-and-delete.
            const string lua = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
            try { await _db.ScriptEvaluateAsync(lua, new RedisKey[] { _key }, new RedisValue[] { _token }); }
            catch { /* ignore */ }
        }
    }
}
