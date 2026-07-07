using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class SoftphoneCallLogIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<SoftphoneCallLog>("softphone_call_logs");
        var indexModels = new List<CreateIndexModel<SoftphoneCallLog>>
        {
            new(Builders<SoftphoneCallLog>.IndexKeys
                .Ascending(s => s.TenantId)
                .Ascending(s => s.UserId)
                .Descending(s => s.StartedAt))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
