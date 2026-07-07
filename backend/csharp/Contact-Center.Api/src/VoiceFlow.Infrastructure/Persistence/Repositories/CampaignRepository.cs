using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : MongoRepository<Campaign>, ICampaignRepository
{
    public CampaignRepository(MongoDbContext context) : base(context, "campaigns") { }

    public async Task<IEnumerable<Campaign>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Campaign>.Filter.Eq(c => c.TenantId, tenantId);
        var sort = Builders<Campaign>.Sort.Descending(c => c.CreatedAt);
        return await Collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<Campaign?> GetByIdForTenantAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Campaign>.Filter.And(
            Builders<Campaign>.Filter.Eq(c => c.Id, id),
            Builders<Campaign>.Filter.Eq(c => c.TenantId, tenantId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Campaign?> ApplyTargetCounterDeltasAsync(
        string id,
        string tenantId,
        IReadOnlyDictionary<string, long> deltas,
        CancellationToken cancellationToken = default)
    {
        if (deltas.Count == 0)
        {
            return await GetByIdForTenantAsync(id, tenantId, cancellationToken);
        }

        var updates = new List<UpdateDefinition<Campaign>>();
        foreach (var (statusKey, delta) in deltas)
        {
            if (delta == 0) continue;
            var field = MapStatusToCounterField(statusKey);
            if (field is null) continue;
            updates.Add(Builders<Campaign>.Update.Inc(field, delta));
        }
        updates.Add(Builders<Campaign>.Update.Inc(c => c.Version, 1L));
        updates.Add(Builders<Campaign>.Update.Set(c => c.LastActivityAt, DateTime.UtcNow));
        updates.Add(Builders<Campaign>.Update.Set(c => c.UpdatedAt, DateTime.UtcNow));

        var filter = Builders<Campaign>.Filter.And(
            Builders<Campaign>.Filter.Eq(c => c.Id, id),
            Builders<Campaign>.Filter.Eq(c => c.TenantId, tenantId));

        return await Collection.FindOneAndUpdateAsync(
            filter,
            Builders<Campaign>.Update.Combine(updates),
            new FindOneAndUpdateOptions<Campaign> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    private static string? MapStatusToCounterField(string status) => status switch
    {
        "total" => "targetsTotal",
        "pending" => "targetsPending",
        "called" => "targetsCalled",
        "successful" => "targetsSuccessful",
        "failed" => "targetsFailed",
        "callback" => "targetsCallback",
        _ => null,
    };
}
