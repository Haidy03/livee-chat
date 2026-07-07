using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class GroupRepository : MongoRepository<Group>, IGroupRepository
{
    public GroupRepository(MongoDbContext context) : base(context, "groups") { }

    public async Task<IEnumerable<Group>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Group>.Filter.Eq(g => g.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
