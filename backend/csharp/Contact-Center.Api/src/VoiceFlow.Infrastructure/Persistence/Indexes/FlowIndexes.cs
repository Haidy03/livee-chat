using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class FlowIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Flow>("flows");
        var indexModels = new List<CreateIndexModel<Flow>>
        {
            new(Builders<Flow>.IndexKeys.Ascending(f => f.TenantId)),
            new(Builders<Flow>.IndexKeys.Ascending(f => f.TenantId).Ascending(f => f.AssignedExtension))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
