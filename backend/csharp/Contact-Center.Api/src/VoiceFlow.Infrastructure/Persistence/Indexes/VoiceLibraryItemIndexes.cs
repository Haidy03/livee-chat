using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class VoiceLibraryItemIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<VoiceLibraryItem>("voice_library_items");
        var indexModels = new List<CreateIndexModel<VoiceLibraryItem>>
        {
            new(Builders<VoiceLibraryItem>.IndexKeys.Ascending(v => v.TenantId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
