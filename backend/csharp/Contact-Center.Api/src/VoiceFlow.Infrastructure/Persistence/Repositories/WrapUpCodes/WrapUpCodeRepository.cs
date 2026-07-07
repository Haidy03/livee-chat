using MongoDB.Driver;
using VoiceFlow.Core.Entities.WrapUpCodes;
using VoiceFlow.Core.Interfaces.Repositories.WrapUpCodes;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.WrapUpCodes;

public sealed class WrapUpCodeRepository : MongoRepository<WrapUpCode>, IWrapUpCodeRepository
{
    public WrapUpCodeRepository(MongoDbContext context) : base(context, "wrapup_codes") { }

    public async Task<IReadOnlyList<WrapUpCode>> ListAsync(string tenantId, bool activeOnly, CancellationToken ct)
    {
        var filter = Builders<WrapUpCode>.Filter.Eq(x => x.TenantId, tenantId);
        if (activeOnly)
            filter &= Builders<WrapUpCode>.Filter.Eq(x => x.IsActive, true);

        return await Collection.Find(filter)
            .SortBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .ToListAsync(ct);
    }

    public async Task<WrapUpCode?> GetAsync(string tenantId, string id, CancellationToken ct) =>
        await Collection.Find(x => x.TenantId == tenantId && x.Id == id).FirstOrDefaultAsync(ct);


    public async Task UpsertAsync(WrapUpCode entity, CancellationToken ct)
    {
        var filter = Builders<WrapUpCode>.Filter.And(
            Builders<WrapUpCode>.Filter.Eq(x => x.TenantId, entity.TenantId),
            Builders<WrapUpCode>.Filter.Eq(x => x.Id, entity.Id));
        await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct)
    {
        var result = await Collection.DeleteOneAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        return result.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<WrapUpCode>> GetByIdsAsync(string tenantId, IEnumerable<string> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<WrapUpCode>();
        var filter = Builders<WrapUpCode>.Filter.And(
            Builders<WrapUpCode>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<WrapUpCode>.Filter.In(x => x.Id, idList));
        return await Collection.Find(filter).ToListAsync(ct);
    }
}
