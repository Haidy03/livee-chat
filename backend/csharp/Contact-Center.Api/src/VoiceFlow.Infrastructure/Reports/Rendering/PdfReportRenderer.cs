using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// PDF export — builds the same self-contained HTML as the HTML renderer, then prints
/// it to PDF through the shared headless Chromium (vector charts + selectable text).
/// </summary>
public sealed class PdfReportRenderer : IReportFormatRenderer
{
    private readonly ReportHtmlBuilder _builder;
    private readonly ChromiumPageRenderer _chromium;

    public PdfReportRenderer(ReportHtmlBuilder builder, ChromiumPageRenderer chromium)
    {
        _builder = builder;
        _chromium = chromium;
    }

    public ExportFormat Format => ExportFormat.Pdf;

    public async Task<RenderedReport> RenderAsync(ReportResult result, ReportRenderOptions options, CancellationToken ct)
    {
        var html = _builder.Build(result, options);
        var pdf = await _chromium.RenderPdfAsync(html, ct);
        return new RenderedReport(pdf, "application/pdf", $"{options.FileStem}.pdf");
    }
}
