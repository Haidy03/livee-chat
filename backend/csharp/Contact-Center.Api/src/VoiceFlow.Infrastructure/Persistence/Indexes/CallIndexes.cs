using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class CallIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Call>("calls");
        var indexModels = new List<CreateIndexModel<Call>>
        {
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Descending(c => c.StartedAt)),
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.Status)),
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.Direction)),
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.TagIds)),
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.Caller)),
            new(Builders<Call>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.EndedAt))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
