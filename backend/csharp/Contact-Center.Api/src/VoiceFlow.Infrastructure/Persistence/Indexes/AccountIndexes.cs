using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class AccountIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Account>("accounts");
        var indexModels = new List<CreateIndexModel<Account>>
        {
            new(Builders<Account>.IndexKeys.Ascending(a => a.UserId))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
