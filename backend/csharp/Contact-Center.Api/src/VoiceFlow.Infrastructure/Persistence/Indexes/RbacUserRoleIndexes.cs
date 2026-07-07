using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class RbacUserRoleIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<RbacUserRole>("rbac_user_roles");
        var indexModels = new List<CreateIndexModel<RbacUserRole>>
        {
            new(Builders<RbacUserRole>.IndexKeys
                .Ascending(r => r.TenantId)
                .Ascending(r => r.UserId)),
            new(Builders<RbacUserRole>.IndexKeys
                .Ascending(r => r.TenantId)
                .Ascending(r => r.RoleId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
