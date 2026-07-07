using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class InvoiceIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<Invoice>("invoices");
        var indexModels = new List<CreateIndexModel<Invoice>>
        {
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.TenantId).Descending(i => i.IssueDate)),
            new(Builders<Invoice>.IndexKeys.Ascending(i => i.TenantId).Ascending(i => i.Status))
        };
        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
