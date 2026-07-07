using MongoDB.Driver;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class BillingIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Billing>("billing");
        var indexModels = new List<CreateIndexModel<Billing>>
        {
            new(
                Builders<Billing>.IndexKeys.Ascending(b => b.TenantId),
                new CreateIndexOptions { Unique = true }
            )
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
