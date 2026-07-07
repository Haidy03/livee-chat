using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class RbacRoleRepository : MongoRepository<RbacRole>, IRbacRoleRepository
{
    public RbacRoleRepository(MongoDbContext context) : base(context, "rbac_roles") { }

    public async Task<IEnumerable<RbacRole>> GetByTenantAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return Enumerable.Empty<RbacRole>();

        var filter = Builders<RbacRole>.Filter.Or(
            Builders<RbacRole>.Filter.Eq(r => r.TenantId, tenantId),
            Builders<RbacRole>.Filter.Eq(r => r.IsSystem, true)
        );        //var filter = tenantId is null
        //    ? Builders<RbacRole>.Filter.Where(r => r.TenantId == null)
        //    : Builders<RbacRole>.Filter.Or(
        //        Builders<RbacRole>.Filter.Eq(r => r.TenantId, tenantId),
        //        Builders<RbacRole>.Filter.Where(r => r.TenantId == null));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
