using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Core.Interfaces.Repositories.Reports;

public interface IReportRunRepository
{
    Task<PagedResult<ReportRun>> ListAsync(string tenantId, string reportId, int page, int pageSize, CancellationToken ct);
    Task AddAsync(ReportRun run, CancellationToken ct);
    Task<ReportRun?> GetAsync(string id, CancellationToken ct);
    Task UpdateReportAsync(ReportRun run, CancellationToken ct);
}
