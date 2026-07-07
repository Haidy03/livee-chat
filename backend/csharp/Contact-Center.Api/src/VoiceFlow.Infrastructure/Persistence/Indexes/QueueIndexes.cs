using MongoDB.Driver;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class QueueIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Queue>("queues");
        var indexModels = new List<CreateIndexModel<Queue>>
        {
            new(Builders<Queue>.IndexKeys.Ascending(q => q.TenantId)),
            new(
                Builders<Queue>.IndexKeys.Ascending(q => q.TenantId).Ascending(q => q.Code),
                new CreateIndexOptions { Unique = true, Name = "uq_tenant_code" })
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}

