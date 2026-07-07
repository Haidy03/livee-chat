using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class CampaignReceivedCallRepository : MongoRepository<CampaignReceivedCallItem>, ICampaignReceivedCallRepository
{
    public CampaignReceivedCallRepository(MongoDbContext context) : base(context, "campaign_received_calls") { }

    public async Task<CampaignReceivedCallPage> ListAsync(string tenantId, string campaignId, int page, int pageSize, CancellationToken ct = default)
    {
        var p = Math.Max(1, page);
        var s = Math.Clamp(pageSize, 1, 200);
        var f = Builders<CampaignReceivedCallItem>.Filter.And(
            Builders<CampaignReceivedCallItem>.Filter.Eq(r => r.TenantId, tenantId),
            Builders<CampaignReceivedCallItem>.Filter.Eq(r => r.CampaignId, campaignId));

        var totalTask = Collection.CountDocumentsAsync(f, cancellationToken: ct);
        var itemsTask = Collection.Find(f)
            .Sort(Builders<CampaignReceivedCallItem>.Sort.Descending(r => r.CreatedAt))
            .Skip((p - 1) * s)
            .Limit(s)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itemsTask);
        return new CampaignReceivedCallPage { Items = itemsTask.Result, TotalCount = totalTask.Result };
    }

    public async Task<long> InsertManyAsync(IReadOnlyList<CampaignReceivedCallItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return 0;
        await Collection.InsertManyAsync(items, new InsertManyOptions { IsOrdered = false }, ct);
        return items.Count;
    }

    public async Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default)
    {
        var f = Builders<CampaignReceivedCallItem>.Filter.And(
            Builders<CampaignReceivedCallItem>.Filter.Eq(r => r.TenantId, tenantId),
            Builders<CampaignReceivedCallItem>.Filter.Eq(r => r.CampaignId, campaignId));
        var res = await Collection.DeleteManyAsync(f, ct);
        return res.DeletedCount;
    }
}
