using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;
namespace VoiceFlow.Infrastructure.Persistence.Repositories.Reports;

public sealed class ReportRepository : MongoRepository<Report>, IReportRepository
{
    public ReportRepository(MongoDbContext context) : base(context, "reports") { }

    public async Task<PagedResult<Report>> ListAsync(string tenantId, ReportListQuery q, CancellationToken ct)
    {
        var fb = Builders<Report>.Filter;
        var filter = fb.Eq(x => x.TenantId, tenantId);

        if (q.Category is not null) filter &= fb.Eq(x => x.Category, q.Category.Value);
        if (q.Status is not null) filter &= fb.Eq(x => x.Status, q.Status.Value);
        if (q.Starred is not null) filter &= fb.Eq(x => x.Starred, q.Starred.Value);
        if (!string.IsNullOrWhiteSpace(q.OwnerId)) filter &= fb.Eq(x => x.OwnerId, q.OwnerId);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var regex = new BsonRegularExpression(q.Search, "i");
            filter &= fb.Or(
                fb.Regex("Name.En", regex),
                fb.Regex("Name.Ar", regex),
                fb.Regex("Description.En", regex),
                fb.Regex("Description.Ar", regex));
        }

        var sort = q.Sort switch
        {
            "name" => Builders<Report>.Sort.Ascending(x => x.Name.En),
            "-name" => Builders<Report>.Sort.Descending(x => x.Name.En),
            "lastRun" => Builders<Report>.Sort.Ascending(x => x.LastRunAt),
            "-lastRun" => Builders<Report>.Sort.Descending(x => x.LastRunAt),
            "runs" => Builders<Report>.Sort.Ascending(x => x.RunsCount),
            "-runs" => Builders<Report>.Sort.Descending(x => x.RunsCount),
            _ => Builders<Report>.Sort.Descending(x => x.UpdatedAt),
        };

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection.Find(filter).Sort(sort)
            .Skip((q.Page - 1) * q.PageSize)
            .Limit(q.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Report>(items, total, q.Page, q.PageSize);
    }

    public Task<Report?> GetAsync(string tenantId, string id, CancellationToken ct) =>
        Collection.Find(x => x.TenantId == tenantId && x.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task AddAsync(Report report, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(report.Id))
            report.Id = ObjectId.GenerateNewId().ToString();
        await Collection.InsertOneAsync(report, cancellationToken: ct);
    }

    public async Task<bool> UpdateReportAsync(Report report, CancellationToken ct)
    {
        var res = await Collection.ReplaceOneAsync(
            x => x.TenantId == report.TenantId && x.Id == report.Id,
            report, cancellationToken: ct);
        return res.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct)
    {
        var res = await Collection.DeleteOneAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        return res.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<Report>> BulkSetStatusAsync(string tenantId, IReadOnlyList<string> ids, ReportStatus status, DateTime now, CancellationToken ct)
    {
        var filter = Builders<Report>.Filter.Eq(x => x.TenantId, tenantId) &
                     Builders<Report>.Filter.In(x => x.Id, ids);
        var update = Builders<Report>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, now);
        await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return await Collection.Find(filter).ToListAsync(ct);
    }

    public async Task<List<Report>> GetDueReportsAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Collection
            .Find(x =>
                x.Status == ReportStatus.Active &&
                x.Schedule.Enabled &&
                x.NextRunAt <= now)
            .ToListAsync(ct);
    }

}
