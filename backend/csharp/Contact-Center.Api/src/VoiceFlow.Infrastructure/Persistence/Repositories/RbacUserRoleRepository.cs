using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class RbacUserRoleRepository : MongoRepository<RbacUserRole>, IRbacUserRoleRepository
{
    public RbacUserRoleRepository(MongoDbContext context) : base(context, "rbac_user_roles") { }

    public async Task<IEnumerable<RbacUserRole>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RbacUserRole>.Filter.Eq(r => r.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RbacUserRole>> GetByUserAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RbacUserRole>.Filter.And(
            Builders<RbacUserRole>.Filter.Eq(r => r.UserId, userId),
            Builders<RbacUserRole>.Filter.Eq(r => r.TenantId, tenantId));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RbacUserRole>> GetByRoleAndTenantAsync(string roleId, string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RbacUserRole>.Filter.And(
            Builders<RbacUserRole>.Filter.Eq(r => r.RoleId, roleId),
            Builders<RbacUserRole>.Filter.Eq(r => r.TenantId, tenantId));
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task DeleteByUserAndTenantAsync(string userId, string tenantId, string roleId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RbacUserRole>.Filter.And(
            Builders<RbacUserRole>.Filter.Eq(r => r.UserId, userId),
            Builders<RbacUserRole>.Filter.Eq(r => r.TenantId, tenantId),
            Builders<RbacUserRole>.Filter.Eq(r => r.RoleId, roleId));
        await Collection.DeleteOneAsync(filter, cancellationToken);
    }
}
