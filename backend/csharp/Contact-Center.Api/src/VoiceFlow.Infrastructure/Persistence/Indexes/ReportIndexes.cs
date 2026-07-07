using MongoDB.Driver;
using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Infrastructure.Persistence.Indexes
{
    public static class ReportIndexes
    {
        public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
        {
            var collection = context.GetCollection<Report>("reports");

            var indexModels = new List<CreateIndexModel<Report>>
        {
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.Status)),

            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Descending(x => x.UpdatedAt)),

            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.Starred)),

            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.OwnerId)),

            new CreateIndexModel<Report>(
        Builders<Report>.IndexKeys
            .Ascending(x => x.NextRunAt)
            .Ascending(x => x.Status))
        };

            await collection.Indexes.CreateManyAsync(indexModels, ct);
        }
    }
}
