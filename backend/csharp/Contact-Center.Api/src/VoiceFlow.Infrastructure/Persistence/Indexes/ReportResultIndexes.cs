using MongoDB.Driver;
using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Infrastructure.Persistence.Indexes;

public static class ReportResultIndexes
{
    public static async Task CreateAsync(
        MongoDbContext context,
        CancellationToken ct = default)
    {
        var collection = context.GetCollection<ReportResult>("report_results");

        var indexModels = new List<CreateIndexModel<ReportResult>>
        {
            new(
                Builders<ReportResult>.IndexKeys
                    .Ascending(x => x.TenantId)
                    .Ascending(x => x.ReportId)
                    .Descending(x => x.GeneratedAt)),

            new(
                Builders<ReportResult>.IndexKeys
                    .Ascending(x => x.RunId),
                new CreateIndexOptions
                {
                    Unique = true
                })
        };

        await collection.Indexes.CreateManyAsync(indexModels, ct);
    }
}
