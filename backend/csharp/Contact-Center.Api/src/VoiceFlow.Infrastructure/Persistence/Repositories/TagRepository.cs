using MongoDB.Driver;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;
using TagEntity = VoiceFlow.Core.Entities.Tag;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class TagRepository : MongoRepository<TagEntity>, ITagRepository
{
    public TagRepository(MongoDbContext context) : base(context, "tags") { }

    public async Task<IEnumerable<TagEntity>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TagEntity>.Filter.Eq(t => t.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
