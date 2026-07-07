using MongoDB.Driver;
using VoiceFlow.Core.Interfaces.Repositories;
using AutoTagEntity = VoiceFlow.Core.Entities.AutoTag;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class AutoTagRepository : MongoRepository<AutoTagEntity>, IAutoTagRepository
{
    public AutoTagRepository(MongoDbContext context) : base(context, "auto_tags") { }

    public async Task<IEnumerable<AutoTagEntity>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AutoTagEntity>.Filter.Eq(t => t.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
