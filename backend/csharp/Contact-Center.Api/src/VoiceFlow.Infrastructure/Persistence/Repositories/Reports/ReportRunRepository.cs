using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.Reports;

public sealed class ReportRunRepository : MongoRepository<ReportRun>, IReportRunRepository
{
    public ReportRunRepository(MongoDbContext context) : base(context, "report_runs") { }

    public async Task<PagedResult<ReportRun>> ListAsync(string tenantId, string reportId, int page, int pageSize, CancellationToken ct)
    {
        var filter = Builders<ReportRun>.Filter.Eq(x => x.TenantId, tenantId) &
                     Builders<ReportRun>.Filter.Eq(x => x.ReportId, reportId);
        var sort = Builders<ReportRun>.Sort.Descending(x => x.StartedAt);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection.Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ReportRun>(items, total, page, pageSize);
    }

    public async Task AddAsync(ReportRun run, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(run.Id))
            run.Id = ObjectId.GenerateNewId().ToString();
        await Collection.InsertOneAsync(run, cancellationToken: ct);
    }

    public Task<ReportRun?> GetAsync(string id, CancellationToken ct) =>
        Collection.Find(Builders<ReportRun>.Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync(ct)!;

    public Task UpdateReportAsync(ReportRun run, CancellationToken ct) =>
        Collection.ReplaceOneAsync(Builders<ReportRun>.Filter.Eq(x => x.Id, run.Id), run, cancellationToken: ct);
}
