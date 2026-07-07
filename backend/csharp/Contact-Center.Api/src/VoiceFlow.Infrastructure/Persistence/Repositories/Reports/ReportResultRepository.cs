using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.Reports
{
    public sealed class ReportResultRepository : MongoRepository<ReportResult>,IReportResultRepository
    {
        public ReportResultRepository(MongoDbContext context) : base(context, "report_results") { }

        public async Task AddAsync(ReportResult result, CancellationToken ct)
        {
            await Collection.InsertOneAsync(result, cancellationToken: ct);
        }

        public Task<ReportResult?> GetByRunIdAsync(string tenantId, string runId, CancellationToken ct) =>
            Collection.Find(Builders<ReportResult>.Filter.Eq(x => x.TenantId, tenantId) &
                      Builders<ReportResult>.Filter.Eq(x => x.RunId, runId)).FirstOrDefaultAsync(ct)!;

        public Task<ReportResult?> GetLatestForReportAsync(string tenantId, string reportId, CancellationToken ct) =>
            Collection.Find(Builders<ReportResult>.Filter.Eq(x => x.TenantId, tenantId) &
                      Builders<ReportResult>.Filter.Eq(x => x.ReportId, reportId))
                .SortByDescending(x => x.GeneratedAt)
                .FirstOrDefaultAsync(ct)!;
    }

}
