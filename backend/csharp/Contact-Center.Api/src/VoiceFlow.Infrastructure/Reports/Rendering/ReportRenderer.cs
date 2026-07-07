using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>Routes a render request to the <see cref="IReportFormatRenderer"/> registered for that format.</summary>
public sealed class ReportRenderer : IReportRenderer
{
    private readonly IReadOnlyDictionary<ExportFormat, IReportFormatRenderer> _byFormat;

    public ReportRenderer(IEnumerable<IReportFormatRenderer> renderers)
    {
        _byFormat = renderers.ToDictionary(r => r.Format);
    }

    public bool CanRender(ExportFormat format) => _byFormat.ContainsKey(format);

    public Task<RenderedReport> RenderAsync(ReportResult result, ExportFormat format, ReportRenderOptions options, CancellationToken ct)
    {
        if (!_byFormat.TryGetValue(format, out var renderer))
            throw new NotSupportedException($"No report renderer registered for format '{format}'.");

        return renderer.RenderAsync(result, options, ct);
    }
}
