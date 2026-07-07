using MongoDB.Driver;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class CampaignTargetIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<CampaignTarget>("campaign_targets");
        var models = new List<CreateIndexModel<CampaignTarget>>
        {
            new(Builders<CampaignTarget>.IndexKeys
                .Ascending(t => t.TenantId)
                .Ascending(t => t.CampaignId)
                .Ascending(t => t.Status)),
            new(Builders<CampaignTarget>.IndexKeys
                .Ascending(t => t.TenantId)
                .Ascending(t => t.CampaignId)
                .Ascending(t => t.Phone)),
            new(Builders<CampaignTarget>.IndexKeys
                .Ascending(t => t.TenantId)
                .Ascending(t => t.CampaignId)
                .Descending(t => t.CreatedAt)),
        };
        await collection.Indexes.CreateManyAsync(models, ct);
    }
}

public static class CampaignActivityIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<CampaignActivityItem>("campaign_activity");
        var models = new List<CreateIndexModel<CampaignActivityItem>>
        {
            new(Builders<CampaignActivityItem>.IndexKeys
                .Ascending(a => a.TenantId)
                .Ascending(a => a.CampaignId)
                .Descending(a => a.CreatedAt)),
        };
        await collection.Indexes.CreateManyAsync(models, ct);
    }
}

public static class CampaignReceivedCallIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<CampaignReceivedCallItem>("campaign_received_calls");
        var models = new List<CreateIndexModel<CampaignReceivedCallItem>>
        {
            new(Builders<CampaignReceivedCallItem>.IndexKeys
                .Ascending(r => r.TenantId)
                .Ascending(r => r.CampaignId)
                .Descending(r => r.CreatedAt)),
        };
        await collection.Indexes.CreateManyAsync(models, ct);
    }
}
