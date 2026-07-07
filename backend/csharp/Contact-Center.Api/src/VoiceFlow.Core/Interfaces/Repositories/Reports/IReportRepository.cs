using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Interfaces.Repositories.Reports;

public sealed record ReportListQuery(
    string? Search,
    ReportCategory? Category,
    ReportStatus? Status,
    bool? Starred,
    string? OwnerId,
    int Page,
    int PageSize,
    string? Sort);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long Total, int Page, int PageSize);

public interface IReportRepository
{
    Task<PagedResult<Report>> ListAsync(string tenantId, ReportListQuery query, CancellationToken ct);
    Task<Report?> GetAsync(string tenantId, string id, CancellationToken ct);
    Task AddAsync(Report report, CancellationToken ct);
    Task<bool> UpdateReportAsync(Report report, CancellationToken ct);
    Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct);
    Task<List<Report>> GetDueReportsAsync(DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<Report>> BulkSetStatusAsync(string tenantId, IReadOnlyList<string> ids, ReportStatus status, DateTime now, CancellationToken ct);
}
