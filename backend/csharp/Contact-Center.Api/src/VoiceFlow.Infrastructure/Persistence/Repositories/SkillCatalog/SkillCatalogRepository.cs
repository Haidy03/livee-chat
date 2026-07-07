using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities.SkillCatalog;
using VoiceFlow.Core.Interfaces.Repositories.SkillCatalog;
using VoiceFlow.Infrastructure.Persistence;
using VoiceFlow.Infrastructure.Persistence.Repositories;
namespace VoiceFlow.Infrastructure.Repositories.SkillCatalog;

public sealed class SkillCatalogRepository : MongoRepository<SkillCategory>, ISkillCatalogRepository
{
    public SkillCatalogRepository(MongoDbContext context) : base(context, "skills_catalog") { }
    public async Task<IReadOnlyList<SkillCategory>> ListAsync(
        string tenantId,
        CancellationToken ct)
    {
        return await Collection
            .Find(x => x.TenantId == tenantId)
            .SortBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<SkillCategory?> GetAsync(
        string tenantId,
        string categoryId,
        CancellationToken ct)
    {
        return await Collection
            .Find(x =>
                x.TenantId == tenantId &&
                x.Id == categoryId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task ReplaceAllAsync(
        string tenantId,
        IReadOnlyList<SkillCategory> categories,
        CancellationToken ct)
    {
        await Collection.DeleteManyAsync(
            x => x.TenantId == tenantId,
            ct);

        if (categories.Count == 0)
            return;

        foreach (var category in categories)
        {
            category.TenantId = tenantId;
        }

        await Collection.InsertManyAsync(
            categories,
            cancellationToken: ct);
    }

    public async Task UpsertAsync(
        SkillCategory category,
        CancellationToken ct)
    {
        var filter = Builders<SkillCategory>.Filter.And(
            Builders<SkillCategory>.Filter.Eq(x => x.TenantId, category.TenantId),
            Builders<SkillCategory>.Filter.Eq(x => x.Id, category.Id));

        await Collection.ReplaceOneAsync(
            filter,
            category,
            new ReplaceOptions
            {
                IsUpsert = true
            },
            ct);
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        string categoryId,
        CancellationToken ct)
    {
        var result = await Collection.DeleteOneAsync(
            x =>
                x.TenantId == tenantId &&
                x.Id == categoryId,
            ct);

        return result.DeletedCount > 0;
    }
}
