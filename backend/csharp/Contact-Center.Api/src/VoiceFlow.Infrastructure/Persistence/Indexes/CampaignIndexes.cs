using MongoDB.Driver;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class CampaignIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Campaign>("campaigns");
        var indexModels = new List<CreateIndexModel<Campaign>>
        {
            new(Builders<Campaign>.IndexKeys
                .Ascending(c => c.TenantId)
                .Descending(c => c.CreatedAt)),
            new(Builders<Campaign>.IndexKeys
                .Ascending(c => c.TenantId)
                .Ascending(c => c.Status)),
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
