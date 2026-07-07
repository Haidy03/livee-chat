using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class ContactIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Contact>("contacts");
        var indexModels = new List<CreateIndexModel<Contact>>
        {
            new(Builders<Contact>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.Phone)),
            new(Builders<Contact>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.Name)),
            new(Builders<Contact>.IndexKeys.Ascending(c => c.TenantId).Ascending(c => c.TagIds))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
