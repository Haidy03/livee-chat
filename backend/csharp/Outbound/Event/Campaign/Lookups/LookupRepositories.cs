using System.Collections.Concurrent;
using HelperLib.DataBase;
using HelperLib.Models.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Lookups;

/// <summary>
/// Reads per-tenant trunk + caller id from the contact-center accounts collection. Cached
/// in-process for 30s. A blank <c>outboundTrunk</c> is a valid configuration — it means "dial
/// PJSIP endpoints directly" — so the returned <see cref="TenantTrunkInfo.Trunk"/> may be empty.
/// A null return means the account row itself is missing (hard fail).
/// </summary>
public sealed class TenantTrunkRepository : ITenantTrunkRepository
{
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ConcurrentDictionary<string, (DateTime exp, TenantTrunkInfo? val)> _cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public TenantTrunkRepository(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _accounts = db.GetCollection<BsonDocument>("accounts");
    }

    public async Task<TenantTrunkInfo?> GetAsync(string tenantId, CancellationToken ct)
    {
        if (_cache.TryGetValue(tenantId, out var hit) && hit.exp > DateTime.UtcNow)
            return hit.val;

        var doc = await _accounts.Find(Builders<BsonDocument>.Filter.Eq("_id", tenantId))
            .FirstOrDefaultAsync(ct);

        TenantTrunkInfo? value = null;
        if (doc != null)
        {
            var trunkVal = doc.GetValue("outboundTrunk", BsonNull.Value);
            var cidVal = doc.GetValue("outboundCallerId", BsonNull.Value);
            var trunk = trunkVal.IsBsonNull ? string.Empty : trunkVal.AsString;
            var cid = cidVal.IsBsonNull ? string.Empty : cidVal.AsString;
            value = new TenantTrunkInfo(trunk ?? string.Empty, cid ?? string.Empty);
        }

        _cache[tenantId] = (DateTime.UtcNow.Add(Ttl), value);
        return value;
    }
}

public sealed class CampaignLookupRepository : ICampaignLookupRepository
{
    private readonly IMongoCollection<CampaignModel> _campaigns;
    private readonly ConcurrentDictionary<string, (DateTime exp, CampaignDialingInfo? val)> _cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public CampaignLookupRepository(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _campaigns = db.GetCollection<CampaignModel>("campaigns");
    }

    public async Task<CampaignDialingInfo?> GetAsync(string campaignId, CancellationToken ct)
    {
        if (_cache.TryGetValue(campaignId, out var hit) && hit.exp > DateTime.UtcNow)
            return hit.val;

        var doc = await _campaigns.Find(Builders<CampaignModel>.Filter.Eq(c => c.Id, campaignId))
            .FirstOrDefaultAsync(ct);

        CampaignDialingInfo? value = doc == null
            ? null
            : new CampaignDialingInfo(
                string.IsNullOrWhiteSpace(doc.DialingMode) ? "progressive" : doc.DialingMode!,
                doc.PowerRatio <= 0 ? 1.0 : doc.PowerRatio,
                doc.QueueId ?? string.Empty,
                doc.TenantId);

        _cache[campaignId] = (DateTime.UtcNow.Add(Ttl), value);
        return value;
    }
}
