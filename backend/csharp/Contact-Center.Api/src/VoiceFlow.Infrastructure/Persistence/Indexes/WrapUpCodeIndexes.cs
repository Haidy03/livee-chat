using MongoDB.Driver;
using VoiceFlow.Core.Entities.WrapUpCodes;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class WrapUpCodeIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var codes = context.GetCollection<WrapUpCode>("wrapup_codes");
        await codes.Indexes.CreateManyAsync(new[]
        {
           /* new CreateIndexModel<WrapUpCode>(
                Builders<WrapUpCode>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true, Name = "uq_tenant_code" }),*/
            new CreateIndexModel<WrapUpCode>(
                Builders<WrapUpCode>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.IsActive)
                    .Ascending(x => x.SortOrder)),
        }, ct);

        var map = context.GetCollection<QueueWrapUpCode>("queue_wrapup_codes");
        await map.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<QueueWrapUpCode>(
                Builders<QueueWrapUpCode>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.QueueId)),
            new CreateIndexModel<QueueWrapUpCode>(
                Builders<QueueWrapUpCode>.IndexKeys.Ascending(x => x.QueueId).Ascending(x => x.WrapUpCodeId),
                new CreateIndexOptions { Unique = true, Name = "uq_queue_code" }),
            new CreateIndexModel<QueueWrapUpCode>(
                Builders<QueueWrapUpCode>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.WrapUpCodeId)),
        }, ct);
    }
}
