using System.Globalization;
using System.Net;
using System.Text.Json;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Interfaces.Reports;

namespace VoiceFlow.Infrastructure.Reports.Rendering;

/// <summary>
/// Builds the self-contained export HTML: the sheet chrome (header/meta/footer + CSS)
/// plus the report export bundle and the run data as JSON. The bundle renders the
/// exact same Recharts view the app shows into <c>#report-root</c>. The document
/// stands alone — opened in a browser it renders client-side; loaded in headless
/// Chromium it is printed to PDF. Ported from the frontend nodeToStandaloneHtml
/// template, minus the live-DOM scraping (the bundle owns the content now).
/// </summary>
public sealed class ReportHtmlBuilder
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Lazy<string> _bundle;

    public ReportHtmlBuilder()
    {
        _bundle = new Lazy<string>(LoadBundle);
    }

    private static string LoadBundle()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Reports", "Rendering", "assets", "report-charts.js");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Report export bundle not found at '{path}'. Run `npm run build:report-bundle` in the web repo.", path);
        return File.ReadAllText(path);
    }

    public string Build(ReportResult result, ReportRenderOptions options)
    {
        var rtl = options.Lang == "ar";
        var dir = rtl ? "rtl" : "ltr";
        var lang = rtl ? "ar" : "en";
        var labels = Labels(rtl);
        var title = WebUtility.HtmlEncode(options.Title);

        var exportedAt = FormatDate(DateTimeOffset.UtcNow, rtl);
        var dataGeneratedAt = options.GeneratedAt is not null && DateTimeOffset.TryParse(options.GeneratedAt, out var gen)
            ? FormatDate(gen, rtl)
            : null;

        var metaParts = new List<string>();
        if (dataGeneratedAt is not null)
            metaParts.Add($"<span class=\"report-meta-item\">{labels.DataGenerated}: {WebUtility.HtmlEncode(dataGeneratedAt)}</span>");
        metaParts.Add($"<span class=\"report-meta-item\">{labels.Exported}: {WebUtility.HtmlEncode(exportedAt)}</span>");
        metaParts.Add($"<span class=\"report-meta-item\">{labels.Rows}: {options.RowCount.ToString("N0", rtl ? CultureInfo.GetCultureInfo("ar") : CultureInfo.InvariantCulture)}</span>");
        var meta = string.Join("<span class=\"report-meta-sep\">·</span>", metaParts);

        var payload = new
        {
            id = result.Id,
            reportId = result.ReportId,
            runId = result.RunId,
            generatedAt = result.GeneratedAt.ToString("o"),
            rowCount = result.RowCount,
            columns = result.Columns.Select(c => new { key = c.Key, label = c.Label, dataType = c.DataType }),
            rows = result.Rows,
            summary = result.Summary,
        };
        // System.Text.Json's default encoder escapes <, >, & to \uXXXX, so the JSON is
        // safe to embed directly inside <script> (no "</script>" can appear).
        var resultJson = JsonSerializer.Serialize(payload, JsonOpts);
        var vizJson = JsonSerializer.Serialize(options.Viz, JsonOpts);
        var langJson = JsonSerializer.Serialize(lang, JsonOpts);

        return $$"""
<!doctype html>
<html dir="{{dir}}" lang="{{lang}}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{{title}}</title>
<style>
:root{
  --rs-bg:#f0ede6;--rs-surface:#fff;--rs-ink:#14171c;--rs-ink2:rgba(20,23,28,.85);
  --rs-muted:#6b7078;--rs-border:#ebe6da;--rs-r-md:10px;--rs-shadow:0 1px 2px rgba(20,23,28,.04);
}
*,*::before,*::after{box-sizing:border-box}
html{overflow-x:hidden}
body{
  margin:0;padding:20px 16px 28px;background:var(--rs-bg);color:var(--rs-ink);
  font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Helvetica,Arial,sans-serif;
  line-height:1.45;-webkit-print-color-adjust:exact;print-color-adjust:exact;
  display:flex;justify-content:center;overflow-x:hidden;
}
.report-sheet{
  width:100%;max-width:920px;background:var(--rs-surface);
  box-shadow:0 1px 4px rgba(20,23,28,.08),0 8px 24px rgba(20,23,28,.06);
  padding:clamp(20px,4vw,44px);display:flex;flex-direction:column;gap:0;
}
.report-header{text-align:center;padding-bottom:10px;margin-bottom:14px;border-bottom:1px solid var(--rs-border)}
.report-brand{font-size:9px;letter-spacing:.12em;text-transform:uppercase;color:#9ca0a6;margin-bottom:6px}
.report-title{margin:0;font-size:20px;font-weight:600;color:var(--rs-ink);line-height:1.25}
.report-meta{margin-top:6px;font-size:10px;color:var(--rs-muted)}
.report-meta-sep{margin:0 6px;opacity:.55}
.report-content{flex:1;min-width:0}
/* rs-* primitives used by the KPI / gauge / table views inside the bundle. */
.rs-card{background:var(--rs-surface);border:1px solid #e6e0d4;border-radius:var(--rs-r-md);box-shadow:var(--rs-shadow)}
.rs-display{letter-spacing:-.01em;font-weight:600}
.rs-num{font-variant-numeric:tabular-nums;font-feature-settings:"tnum"}
.rs-label{font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:var(--rs-muted);font-weight:500}
.p-1{padding:4px}.p-4{padding:16px}.mt-1{margin-top:4px}.mt-2{margin-top:8px}.truncate{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.report-content table{width:100%;border-collapse:collapse;font-size:11px}
.report-content th,.report-content td{padding:5px 7px;text-align:start;vertical-align:top}
.report-content thead th{font-size:10px;text-transform:uppercase;letter-spacing:.04em;color:var(--rs-muted);border-bottom:1px solid var(--rs-border)}
.report-content tbody tr{border-top:1px solid #f0ebdf}
.report-footer{margin-top:18px;padding-top:8px;border-top:1px solid var(--rs-border);display:flex;justify-content:space-between;align-items:center;gap:12px;font-size:9px;color:#9ca0a6}
@page{size:A4 portrait;margin:12mm 10mm}
@media print{
  html,body{background:#fff!important;padding:0!important;margin:0!important;overflow:visible!important}
  .report-sheet{width:100%!important;max-width:none!important;box-shadow:none!important;padding:0!important}
  .report-header,.report-footer{break-inside:avoid-page}
  .report-content tr{break-inside:avoid}
}
</style>
</head>
<body>
<div class="report-sheet">
  <header class="report-header">
    <div class="report-brand">{{labels.Brand}}</div>
    <h1 class="report-title">{{title}}</h1>
    <div class="report-meta">{{meta}}</div>
  </header>
  <main class="report-content"><div id="report-root"></div></main>
  <footer class="report-footer">
    <span>{{labels.Confidential}}</span>
    <span>{{WebUtility.HtmlEncode(exportedAt)}}</span>
  </footer>
</div>
<script>{{_bundle.Value}}</script>
<script>
window.renderReportExport({elementId:"report-root",result:{{resultJson}},viz:{{vizJson}},lang:{{langJson}}});
</script>
</body>
</html>
""";
    }

    private static string FormatDate(DateTimeOffset date, bool rtl)
    {
        var culture = CultureInfo.GetCultureInfo(rtl ? "ar" : "en-US");
        return date.ToLocalTime().ToString("dd MMM yyyy, HH:mm", culture);
    }

    private static (string Brand, string DataGenerated, string Exported, string Rows, string Confidential) Labels(bool rtl) =>
        rtl
            ? ("ريزونانس", "بيانات التقرير", "تاريخ التصدير", "الصفوف", "سري — للاستخدام الداخلي فقط")
            : ("Resonance", "Report data", "Exported", "Rows", "Confidential — internal use only");
}
