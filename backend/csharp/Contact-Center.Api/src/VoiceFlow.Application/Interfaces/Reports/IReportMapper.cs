
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Entities.Reports;

namespace VoiceFlow.Application.Interfaces.Reports;

/// <summary>
/// Abstraction over the AutoMapper-backed mapper so the Application layer
/// stays free of Infrastructure concerns (constitution principle II).
/// </summary>
public interface IReportMapper
{
    Report ToEntity(CreateReportRequest request);
    void Apply(UpdateReportRequest request, Report target);
    ReportResponse ToResponse(Report entity);
    IReadOnlyList<ReportResponse> ToResponse(IEnumerable<Report> entities);
    ReportRunResponse ToRunResponse(ReportRun entity);
    IReadOnlyList<ReportRunResponse> ToRunResponse(IEnumerable<ReportRun> entities);
    ReportResultResponse ToResponse(ReportResult result);
}
