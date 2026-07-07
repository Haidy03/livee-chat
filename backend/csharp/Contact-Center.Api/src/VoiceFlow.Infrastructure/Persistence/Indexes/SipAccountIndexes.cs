using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class SipAccountIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<SipAccount>("sip_accounts");
        var indexModels = new List<CreateIndexModel<SipAccount>>
        {
            new(Builders<SipAccount>.IndexKeys
                .Ascending(s => s.TenantId)
                .Ascending(s => s.UserId),
                new CreateIndexOptions { Unique = true })
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
