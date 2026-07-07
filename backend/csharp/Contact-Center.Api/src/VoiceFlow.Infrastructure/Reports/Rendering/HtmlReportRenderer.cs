using System.Text;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// HTML export — a self-contained document that renders the report's chart (via the
/// embedded bundle) when opened in any browser. No headless engine needed here; the
/// PDF renderer reuses the same document.
/// </summary>
public sealed class HtmlReportRenderer : IReportFormatRenderer
{
    private readonly ReportHtmlBuilder _builder;

    public HtmlReportRenderer(ReportHtmlBuilder builder) => _builder = builder;

    public ExportFormat Format => ExportFormat.Html;

    public Task<RenderedReport> RenderAsync(ReportResult result, ReportRenderOptions options, CancellationToken ct)
    {
        var html = _builder.Build(result, options);
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(html);
        return Task.FromResult(new RenderedReport(bytes, "text/html; charset=utf-8", $"{options.FileStem}.html"));
    }
}
