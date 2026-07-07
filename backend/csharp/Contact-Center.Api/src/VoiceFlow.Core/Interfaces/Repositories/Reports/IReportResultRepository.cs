
using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Core.Interfaces.Repositories.Reports
{
    public interface IReportResultRepository
    {
        Task AddAsync(ReportResult result, CancellationToken ct);
        Task<ReportResult?> GetByRunIdAsync(string tenantId, string runId, CancellationToken ct);
        Task<ReportResult?> GetLatestForReportAsync(string tenantId, string reportId, CancellationToken ct);
    }

}
