using System.Collections.Concurrent;
using HelperLib.DataBase;
using HelperLib.Models.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Pacing;

/// <summary>
/// Rolling per-campaign telemetry the predictive pacer needs, computed from the
/// <c>call_attempts</c> ledger over a recent window and cached briefly (the dispatcher sweeps
/// often). <see cref="CampaignStats.Unknown"/> (connect rate 1.0, no abandonment) is returned on
/// thin data so predictive pacing degrades to 1:1 — never over-dialing on a cold start.
/// </summary>
public sealed record CampaignStats(int Attempts, double ConnectRate, double AbandonRate, double AvgHandleSeconds)
{
    public static readonly CampaignStats Unknown = new(0, 1.0, 0.0, 0.0);
}

public interface ICampaignStatsProvider
{
    Task<CampaignStats> GetAsync(string campaignId, CancellationToken ct);
}

public sealed class CampaignStatsProvider : ICampaignStatsProvider
{
    private const int MinSample = 10;                                  // below this we don't trust the rate
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<BsonDocument> _attempts;
    private readonly ConcurrentDictionary<string, (DateTime exp, CampaignStats val)> _cache = new();

    public CampaignStatsProvider(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _attempts = db.GetCollection<BsonDocument>("call_attempts");
    }

    public async Task<CampaignStats> GetAsync(string campaignId, CancellationToken ct)
    {
        if (_cache.TryGetValue(campaignId, out var hit) && hit.exp > DateTime.UtcNow)
            return hit.val;

        var stats = await ComputeAsync(campaignId, ct);
        _cache[campaignId] = (DateTime.UtcNow.Add(CacheTtl), stats);
        return stats;
    }

    private async Task<CampaignStats> ComputeAsync(string campaignId, CancellationToken ct)
    {
        var since = DateTime.UtcNow - Window;
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "campaignId", campaignId },
                { "startedAt", new BsonDocument("$gte", since) },
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "attempts", new BsonDocument("$sum", 1) },
                { "connected", SumIf(new BsonDocument("$eq", new BsonArray { "$dialStatus", "ANSWER" })) },
                { "abandoned", SumIf(new BsonDocument("$eq", new BsonArray { "$disposition", "abandoned" })) },
                { "durSum", new BsonDocument("$sum", NonNegative("$durationSec")) },
                { "durCount", SumIf(new BsonDocument("$gt", new BsonArray { NonNegative("$durationSec"), 0 })) },
            }),
        };

        var doc = await _attempts.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(ct);
        if (doc is null) return CampaignStats.Unknown;

        var attempts = doc.GetValue("attempts", 0).ToInt32();
        if (attempts < MinSample) return CampaignStats.Unknown;

        var connected = doc.GetValue("connected", 0).ToInt32();
        var abandoned = doc.GetValue("abandoned", 0).ToInt32();
        var durSum = doc.GetValue("durSum", 0).ToDouble();
        var durCount = doc.GetValue("durCount", 0).ToInt32();

        var connectRate = attempts > 0 ? (double)connected / attempts : 1.0;
        var abandonRate = connected > 0 ? (double)abandoned / connected : 0.0;
        var aht = durCount > 0 ? durSum / durCount : 0.0;

        return new CampaignStats(attempts, connectRate, abandonRate, aht);
    }

    private static BsonDocument SumIf(BsonValue cond) =>
        new("$sum", new BsonDocument("$cond", new BsonArray { cond, 1, 0 }));

    private static BsonDocument NonNegative(string field) =>
        new("$ifNull", new BsonArray { field, 0 });
}
