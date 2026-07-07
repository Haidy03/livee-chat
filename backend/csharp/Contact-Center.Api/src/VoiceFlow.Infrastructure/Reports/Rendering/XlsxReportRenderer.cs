using ClosedXML.Excel;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>XLSX export via ClosedXML. Numbers stay numeric; everything else is written as text.</summary>
public sealed class XlsxReportRenderer : IReportFormatRenderer
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ExportFormat Format => ExportFormat.Xlsx;

    public Task<RenderedReport> RenderAsync(ReportResult result, ReportRenderOptions options, CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Report");

        for (var c = 0; c < result.Columns.Count; c++)
        {
            var col = result.Columns[c];
            sheet.Cell(1, c + 1).Value = string.IsNullOrEmpty(col.Label) ? col.Key : col.Label;
        }
        sheet.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < result.Rows.Count; r++)
        {
            var row = result.Rows[r];
            for (var c = 0; c < result.Columns.Count; c++)
            {
                row.TryGetValue(result.Columns[c].Key, out var cell);
                var target = sheet.Cell(r + 2, c + 1);
                if (cell is bool b)
                    target.Value = b;
                else if (ReportCellFormatter.TryGetNumber(cell, out var n))
                    target.Value = n;
                else
                    target.Value = ReportCellFormatter.ToText(cell);
            }
        }

        sheet.Columns().AdjustToContents();

        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        return Task.FromResult(new RenderedReport(buffer.ToArray(), XlsxContentType, $"{options.FileStem}.xlsx"));
    }
}
