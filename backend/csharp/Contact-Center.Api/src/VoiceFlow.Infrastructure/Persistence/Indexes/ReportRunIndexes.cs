using MongoDB.Driver;
using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class ReportRunIndexes
{
    public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
    {
        var collection = context.GetCollection<ReportRun>("report_runs");
        var indexModels = new List<CreateIndexModel<ReportRun>>
        {
            new(
                Builders<ReportRun>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.ReportId)
                    .Descending(x => x.StartedAt)),
        };

        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
