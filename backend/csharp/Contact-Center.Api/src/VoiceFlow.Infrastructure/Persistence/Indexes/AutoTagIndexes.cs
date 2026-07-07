using MongoDB.Driver;
using AutoTagEntity = VoiceFlow.Core.Entities.AutoTag;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class AutoTagIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<AutoTagEntity>("auto_tags");
        var indexModels = new List<CreateIndexModel<AutoTagEntity>>
        {
            new(Builders<AutoTagEntity>.IndexKeys.Ascending(t => t.TenantId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
