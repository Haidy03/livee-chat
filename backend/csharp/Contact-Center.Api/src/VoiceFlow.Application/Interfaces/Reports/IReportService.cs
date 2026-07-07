

using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Application.Interfaces.Reports;

public interface IReportService
{
    Task<Result<PagedResponse<ReportResponse>>> ListAsync(string? search, string? category, string? status, bool? starred, string? ownerId, int page, int pageSize, string? sort, CancellationToken ct);
    Task<Result<ReportResponse>> GetAsync(string id, CancellationToken ct);
    Task<Result<ReportResponse>> CreateAsync(CreateReportRequest request, CancellationToken ct);
    Task<Result<ReportResponse>> UpdateAsync(string id, UpdateReportRequest request, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct);
    Task<Result<IReadOnlyList<ReportResponse>>> BulkSetStatusAsync(BulkStatusRequest request, CancellationToken ct);
    Task<Result<ReportResponse>> RunAsync(string id, CancellationToken ct);
    Task<Result<PagedResponse<ReportRunResponse>>> ListRunsAsync(string reportId, int page, int pageSize, CancellationToken ct);
    Task<Result<ReportResultResponse>> GetRunResultAsync(string reportId, string runId, CancellationToken ct);
    Task<Result<ReportResultResponse>> GetLatestResultAsync(string reportId, CancellationToken ct);
    Task<Result<RenderedReport>> ExportRunResultAsync(string reportId, string runId, ExportFormat format, string lang, CancellationToken ct);
    Result<IReadOnlyList<DataSourceMetadataResponse>> ListDataSources();
    Result<DataSourceMetadataResponse> GetDataSourceMetadata(string key);
}
