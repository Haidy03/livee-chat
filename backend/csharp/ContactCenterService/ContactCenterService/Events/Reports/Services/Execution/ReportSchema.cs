using MongoDB.Bson;
using VoiceFlow.Core.Reports.Catalog;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Thin wrapper over <see cref="ReportDataSourceCatalog"/> — the single source of
/// truth shared with the API/frontend. Replaces the executor's old hand-rolled,
/// drifted field map: logical field keys now resolve to the exact Call BSON element
/// names the catalog declares (e.g. <c>callerNumber → callerId</c>).
/// </summary>
internal sealed class ReportSchema
{
    private readonly ReportDataSourceDefinition? _source;

    public string CollectionName { get; }
    public string DateField { get; }

    /// <summary>Metric-mode reports delegate to this source's collection when set (e.g. agents → calls).</summary>
    public string? MetricDelegateKey => _source?.MetricDelegateKey;

    /// <summary>Metric-mode runs a bespoke rollup builder when set (e.g. campaigns → "campaign").</summary>
    public string? MetricSummaryBuilder => _source?.MetricSummaryBuilder;

    private ReportSchema(string dataSource, ReportDataSourceDefinition? source)
    {
        _source = source;
        CollectionName = source?.CollectionName ?? dataSource;
        DateField = source?.DateField ?? "createdAt";
    }

    public static ReportSchema Resolve(string dataSource) =>
        new(dataSource, ReportDataSourceCatalog.Find(dataSource));

    /// <summary>Logical field/dimension key → physical Mongo element name.</summary>
    public string MapField(string logical) =>
        _source?.FindField(logical)?.MongoField ?? logical;

    /// <summary>Declared data type for a logical field (<c>string</c> when unknown).</summary>
    public string TypeFor(string logical) =>
        _source?.FindField(logical)?.DataType ?? "string";

    /// <summary>Catalog field definition for a logical field/dimension key (<c>null</c> when unknown).</summary>
    public ReportFieldDefinition? FindField(string key) => _source?.FindField(key);

    /// <summary>Catalog metric definition for a logical metric key (<c>null</c> when unknown).</summary>
    public ReportMetricDefinition? FindMetric(string key) => _source?.FindMetric(key);

    /// <summary>Human label for a dimension/metric key, falling back to the key itself.</summary>
    public string Label(string key) =>
        _source?.FindField(key)?.LabelEn ?? _source?.FindMetric(key)?.LabelEn ?? key;

    /// <summary>Physical duration element for this source (e.g. calls → <c>totalSeconds</c>).</summary>
    public string DurationField => MapField("durationSec");

    /// <summary>Physical hold-time element for this source (e.g. calls → <c>totalHoldSeconds</c>).</summary>
    public string HoldField => MapField("holdSeconds");

    /// <summary>
    /// Tenant filter every query starts from. Event sources are additionally bounded by the
    /// report's date range; entity sources (agents/queues/campaigns) are a current-state list
    /// and skip the date clause so rows outside the window aren't dropped.
    /// </summary>
    public BsonDocument BuildBaseMatch(string tenantId, (DateTimeOffset from, DateTimeOffset to) range)
    {
        var match = new BsonDocument { { "tenantId", tenantId } };
        if (_source?.DateFiltered ?? true)
            match[DateField] = new BsonDocument { { "$gte", range.from.UtcDateTime }, { "$lt", range.to.UtcDateTime } };
        return match;
    }
}
