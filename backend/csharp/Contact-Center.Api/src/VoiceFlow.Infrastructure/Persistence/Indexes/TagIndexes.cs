using MongoDB.Driver;
using VoiceFlow.Infrastructure.Persistence;
using TagEntity = VoiceFlow.Core.Entities.Tag;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class TagIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<TagEntity>("tags");
        var indexModels = new List<CreateIndexModel<TagEntity>>
        {
            new(Builders<TagEntity>.IndexKeys.Ascending(t => t.TenantId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
