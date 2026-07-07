using StackExchange.Redis;
using VoiceFlow.Api.LiveChat.Application.Abstractions;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Redis;

public sealed class OfferTimeoutStore : IOfferTimeoutStore
{
    private readonly IConnectionMultiplexer _mux;
    public OfferTimeoutStore(IConnectionMultiplexer mux) => _mux = mux;
    private IDatabase Db => _mux.GetDatabase();

    private static string Encode(string requestId, string agentId) => $"{requestId}|{agentId}";
    private static (string RequestId, string AgentId)? Decode(string s)
    {
        var i = s.IndexOf('|');
        if (i <= 0 || i == s.Length - 1) return null;
        return (s[..i], s[(i + 1)..]);
    }

    public Task ArmAsync(string requestId, string agentId, TimeSpan ttl)
    {
        var expiry = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeMilliseconds();
        return Db.SortedSetAddAsync(RedisKeys.Offers, Encode(requestId, agentId), expiry);
    }

    public async Task CancelAsync(string requestId)
    {
        // Remove any member starting with `{requestId}|`
        var all = await Db.SortedSetRangeByScoreAsync(RedisKeys.Offers);
        foreach (var m in all)
        {
            var s = m.ToString();
            if (s.StartsWith(requestId + "|", StringComparison.Ordinal))
                await Db.SortedSetRemoveAsync(RedisKeys.Offers, s);
        }
    }

    public async Task<List<(string RequestId, string AgentId)>> PopExpiredAsync()
    {
        var db = Db;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var expired = await db.SortedSetRangeByScoreAsync(RedisKeys.Offers, double.NegativeInfinity, now);
        var claimed = new List<(string, string)>();
        foreach (var m in expired)
        {
            // Atomic single-worker claim: ZREM returns 1 only for the winner.
            if (await db.SortedSetRemoveAsync(RedisKeys.Offers, m))
            {
                var decoded = Decode(m.ToString());
                if (decoded.HasValue) claimed.Add(decoded.Value);
            }
        }
        return claimed;
    }
}
