using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class CampaignTargetRepository : MongoRepository<CampaignTarget>, ICampaignTargetRepository
{
    public CampaignTargetRepository(MongoDbContext context) : base(context, "campaign_targets") { }

    public async Task<CampaignTargetPage> ListAsync(string tenantId, string campaignId, CampaignTargetListFilter filter, CancellationToken ct = default)
    {
        var builder = Builders<CampaignTarget>.Filter;
        var f = builder.Eq(t => t.TenantId, tenantId) & builder.Eq(t => t.CampaignId, campaignId);
        if (!string.IsNullOrWhiteSpace(filter.Status)) f &= builder.Eq(t => t.Status, filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var rx = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(filter.Search!), "i");
            f &= builder.Or(
                builder.Regex(t => t.FirstName, rx),
                builder.Regex(t => t.LastName, rx),
                builder.Regex(t => t.Phone, rx),
                builder.Regex(t => t.Email, rx));
        }

        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 500);
        var skip = (page - 1) * size;

        var totalTask = Collection.CountDocumentsAsync(f, cancellationToken: ct);
        var itemsTask = Collection.Find(f)
            .Sort(Builders<CampaignTarget>.Sort.Ascending(t => t.CreatedAt))
            .Skip(skip)
            .Limit(size)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itemsTask);
        return new CampaignTargetPage { Items = itemsTask.Result, TotalCount = totalTask.Result };
    }

    public async Task<CampaignTarget?> GetForCampaignAsync(string tenantId, string campaignId, string targetId, CancellationToken ct = default)
    {
        var f = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.TenantId, tenantId),
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId));
        return await Collection.Find(f).FirstOrDefaultAsync(ct);
    }

    public async Task<long> InsertManyAsync(IReadOnlyList<CampaignTarget> targets, CancellationToken ct = default)
    {
        if (targets.Count == 0) return 0;
        await Collection.InsertManyAsync(targets, new InsertManyOptions { IsOrdered = false }, ct);
        return targets.Count;
    }

    public async Task<(string previousStatus, string newStatus)?> UpdateStatusAsync(
        string tenantId, string campaignId, string targetId, string newStatus, string lastCallAtIso, CancellationToken ct = default)
    {
        var f = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.TenantId, tenantId),
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId));

        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, newStatus)
            .Set(t => t.LastCallAt, lastCallAtIso)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        // Capture the previous document (pre-update) so we can compute the counter delta.
        var prev = await Collection.FindOneAndUpdateAsync(
            f,
            update,
            new FindOneAndUpdateOptions<CampaignTarget> { ReturnDocument = ReturnDocument.Before },
            ct);

        if (prev is null) return null;
        return (prev.Status, newStatus);
    }

    public async Task<CampaignTarget?> DeleteForCampaignAsync(string tenantId, string campaignId, string targetId, CancellationToken ct = default)
    {
        var f = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.TenantId, tenantId),
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId));
        return await Collection.FindOneAndDeleteAsync(f, cancellationToken: ct);
    }

    public async Task<IReadOnlyDictionary<string, long>> CountByStatusAsync(string tenantId, string campaignId, CancellationToken ct = default)
    {
        var f = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.TenantId, tenantId),
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId));

        var pipeline = new BsonDocument[]
        {
            new("$match", new BsonDocument { { "tenantId", tenantId }, { "campaignId", campaignId } }),
            new("$group", new BsonDocument { { "_id", "$status" }, { "count", new BsonDocument("$sum", 1) } }),
        };

        var results = await Collection.Aggregate<BsonDocument>(pipeline, cancellationToken: ct).ToListAsync(ct);
        var dict = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var doc in results)
        {
            dict[doc["_id"].AsString] = doc["count"].ToInt64();
        }
        return dict;
    }

    public async Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default)
    {
        var f = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.TenantId, tenantId),
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId));
        var res = await Collection.DeleteManyAsync(f, ct);
        return res.DeletedCount;
    }
}
