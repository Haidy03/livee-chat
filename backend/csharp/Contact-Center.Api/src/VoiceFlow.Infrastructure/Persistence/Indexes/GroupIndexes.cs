using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class GroupIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Group>("groups");
        var indexModels = new List<CreateIndexModel<Group>>
        {
            new(Builders<Group>.IndexKeys.Ascending(g => g.TenantId)),
            new(Builders<Group>.IndexKeys.Ascending(g => g.TenantId).Ascending(g => g.Name))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
