using Microsoft.Extensions.Logging;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Infrastructure.Persistence;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Entry point for running a saved <see cref="Report"/> against MongoDB. Resolves the
/// data source schema from <see cref="VoiceFlow.Core.Reports.Catalog.ReportDataSourceCatalog"/>
/// (the single source of truth shared with the API/frontend) and dispatches on
/// <see cref="ReportDefinition.Mode"/>:
///   * <see cref="ReportMode.Detail"/> → <see cref="DetailReportBuilder"/>.
///   * <see cref="ReportMode.MetricAndDimension"/> → <see cref="MetricReportBuilder"/>.
///
/// Legacy documents without a stored Mode are treated as MetricAndDimension so nothing
/// that used to work stops working.
/// </summary>
public sealed class ReportExecutor
{
    private const string DefaultDataSource = "calls";

    private readonly DetailReportBuilder _detail;
    private readonly MetricReportBuilder _metric;
    private readonly CampaignSummaryBuilder _campaignSummary;

    public ReportExecutor(MongoDbContext voiceFlowDb, ILogger<ReportExecutor>? logger = null)
    {
        _detail = new DetailReportBuilder(voiceFlowDb, logger);
        _metric = new MetricReportBuilder(voiceFlowDb, logger);
        _campaignSummary = new CampaignSummaryBuilder(voiceFlowDb, logger);
    }

    public Task<ReportExecutionOutput> ExecuteAsync(Report report, ReportRunRequested request, CancellationToken ct)
    {
        var def = report.Definition;
        var dataSource = string.IsNullOrWhiteSpace(def.DataSource) ? DefaultDataSource : def.DataSource.Trim();
        var schema = ReportSchema.Resolve(dataSource);
        var range = DateRangeResolver.Resolve(def.Filters.DateRange);

        var mode = def.Mode == ReportMode.Detail ? ReportMode.Detail : ReportMode.MetricAndDimension;

        if (mode == ReportMode.Detail)
            return _detail.BuildAsync(report, request, dataSource, schema, range, ct);

        // Metric mode may run a bespoke cross-collection rollup (e.g. campaigns summary).
        if (!string.IsNullOrWhiteSpace(schema.MetricSummaryBuilder))
            return _campaignSummary.BuildAsync(report, dataSource, range, ct);

        // …or delegate to another collection (e.g. agents → calls grouped by agent).
        if (!string.IsNullOrWhiteSpace(schema.MetricDelegateKey))
        {
            dataSource = schema.MetricDelegateKey!;
            schema = ReportSchema.Resolve(dataSource);
        }
        return _metric.BuildAsync(report, request, dataSource, schema, range, ct);
    }
}
