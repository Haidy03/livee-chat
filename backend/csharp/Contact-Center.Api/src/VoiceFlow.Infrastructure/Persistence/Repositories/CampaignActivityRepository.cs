using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class CampaignActivityRepository : MongoRepository<CampaignActivityItem>, ICampaignActivityRepository
{
    public CampaignActivityRepository(MongoDbContext context) : base(context, "campaign_activity") { }

    public async Task<CampaignActivityPage> ListAsync(string tenantId, string campaignId, int page, int pageSize, CancellationToken ct = default)
    {
        var p = Math.Max(1, page);
        var s = Math.Clamp(pageSize, 1, 200);
        var f = Builders<CampaignActivityItem>.Filter.And(
            Builders<CampaignActivityItem>.Filter.Eq(a => a.TenantId, tenantId),
            Builders<CampaignActivityItem>.Filter.Eq(a => a.CampaignId, campaignId));

        var totalTask = Collection.CountDocumentsAsync(f, cancellationToken: ct);
        var itemsTask = Collection.Find(f)
            .Sort(Builders<CampaignActivityItem>.Sort.Descending(a => a.CreatedAt))
            .Skip((p - 1) * s)
            .Limit(s)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itemsTask);
        return new CampaignActivityPage { Items = itemsTask.Result, TotalCount = totalTask.Result };
    }

    public async Task<long> InsertManyAsync(IReadOnlyList<CampaignActivityItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return 0;
        await Collection.InsertManyAsync(items, new InsertManyOptions { IsOrdered = false }, ct);
        return items.Count;
    }

    public async Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default)
    {
        var f = Builders<CampaignActivityItem>.Filter.And(
            Builders<CampaignActivityItem>.Filter.Eq(a => a.TenantId, tenantId),
            Builders<CampaignActivityItem>.Filter.Eq(a => a.CampaignId, campaignId));
        var res = await Collection.DeleteManyAsync(f, ct);
        return res.DeletedCount;
    }
}
