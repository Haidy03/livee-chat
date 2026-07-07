using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class ProfileIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Profile>("profiles");
        var indexModels = new List<CreateIndexModel<Profile>>
        {
            new(Builders<Profile>.IndexKeys
                .Ascending(p => p.TenantId)
                .Ascending(p => p.UserId),
                new CreateIndexOptions { Unique = true }),
            new(Builders<Profile>.IndexKeys.Ascending(p => p.TenantId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
