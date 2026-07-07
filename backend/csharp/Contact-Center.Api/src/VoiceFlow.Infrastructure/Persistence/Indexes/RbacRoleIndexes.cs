using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class RbacRoleIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<RbacRole>("rbac_roles");
        var indexModels = new List<CreateIndexModel<RbacRole>>
        {
            new(Builders<RbacRole>.IndexKeys.Ascending(r => r.TenantId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
