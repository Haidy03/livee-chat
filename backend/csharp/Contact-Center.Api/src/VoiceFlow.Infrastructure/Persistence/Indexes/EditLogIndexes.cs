using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class EditLogIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<EditLog>("edit_logs");
        var indexModels = new List<CreateIndexModel<EditLog>>
        {
            new(Builders<EditLog>.IndexKeys
                .Ascending(e => e.TenantId)
                .Ascending(e => e.EntityType)
                .Ascending(e => e.EntityId)),
            new(Builders<EditLog>.IndexKeys.Ascending(e => e.TenantId).Descending(e => e.CreatedAt))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
