using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Interfaces.Reports;

/// <summary>A produced export artifact: the raw bytes plus how to serve or attach them.</summary>
public sealed record RenderedReport(byte[] Content, string ContentType, string FileName);

/// <summary>Inputs shared by every format renderer. CSV/XLSX use only <see cref="FileStem"/>;
/// HTML/PDF additionally use the title, visualization, language and run metadata.</summary>
public sealed class ReportRenderOptions
{
    /// <summary>Safe, dated file stem without extension, e.g. "campaign-summary-2026-07-05".</summary>
    public string FileStem { get; init; } = "report";

    /// <summary>Human title shown in the rendered document header.</summary>
    public string Title { get; init; } = "Report";

    /// <summary>Frontend VizId (lowercase): "bar" | "line" | "pie" | "table" | "kpi" | … .</summary>
    public string Viz { get; init; } = "table";

    /// <summary>"en" | "ar" — drives layout direction and locale in the rendered document.</summary>
    public string Lang { get; init; } = "en";

    /// <summary>When the report run's data was generated (ISO 8601), shown in the header meta.</summary>
    public string? GeneratedAt { get; init; }

    /// <summary>Row count shown in the header meta.</summary>
    public int RowCount { get; init; }
}

/// <summary>
/// Renders a computed <see cref="ReportResult"/> into a single export format.
/// One implementation per <see cref="ExportFormat"/>; both the API download path and
/// the scheduled worker reach these through <see cref="IReportRenderer"/>.
/// </summary>
public interface IReportFormatRenderer
{
    ExportFormat Format { get; }
    Task<RenderedReport> RenderAsync(ReportResult result, ReportRenderOptions options, CancellationToken ct);
}

/// <summary>Dispatches a render request to the renderer registered for the requested format.</summary>
public interface IReportRenderer
{
    bool CanRender(ExportFormat format);
    Task<RenderedReport> RenderAsync(ReportResult result, ExportFormat format, ReportRenderOptions options, CancellationToken ct);
}
