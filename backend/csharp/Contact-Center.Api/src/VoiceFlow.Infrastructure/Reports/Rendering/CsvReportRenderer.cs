using System.Globalization;
using System.Text;
using CsvHelper;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// CSV export. UTF-8 with a BOM so Excel opens Arabic/Unicode correctly — same as
/// the old browser export, which prepended "﻿" (exportResult.ts).
/// </summary>
public sealed class CsvReportRenderer : IReportFormatRenderer
{
    public ExportFormat Format => ExportFormat.Csv;

    public Task<RenderedReport> RenderAsync(ReportResult result, ReportRenderOptions options, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        using (var writer = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var col in result.Columns)
                csv.WriteField(HeaderFor(col));
            csv.NextRecord();

            foreach (var row in result.Rows)
            {
                foreach (var col in result.Columns)
                {
                    row.TryGetValue(col.Key, out var cell);
                    csv.WriteField(ReportCellFormatter.ToText(cell));
                }
                csv.NextRecord();
            }
        }

        var rendered = new RenderedReport(buffer.ToArray(), "text/csv", $"{options.FileStem}.csv");
        return Task.FromResult(rendered);
    }

    private static string HeaderFor(ReportResultColumn col) =>
        string.IsNullOrEmpty(col.Label) ? col.Key : col.Label;
}
