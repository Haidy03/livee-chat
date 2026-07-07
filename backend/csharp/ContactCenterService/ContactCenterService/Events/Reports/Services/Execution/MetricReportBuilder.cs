using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Infrastructure.Persistence;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Executes a <see cref="ReportMode.MetricAndDimension"/> report: a <c>$group</c>
/// aggregation over the configured dimensions with one accumulator per metric.
/// Accumulators come from <see cref="MetricAccumulatorBuilder"/>; derived metrics
/// (answer rate) are added via a post-group <c>$project</c>.
/// </summary>
internal sealed class MetricReportBuilder
{
    private const int MaxGroupResults = 1000;

    private readonly MongoDbContext _db;
    private readonly ILogger? _logger;

    public MetricReportBuilder(MongoDbContext db, ILogger? logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReportExecutionOutput> BuildAsync(
        Report report,
        ReportRunRequested request,
        string dataSource,
        ReportSchema schema,
        (DateTimeOffset from, DateTimeOffset to) range,
        CancellationToken ct)
    {
        var def = report.Definition;

        // Reports saved before the mode existed defaulted to a date dimension + count.
        var isLegacy = def.SchemaVersion <= 1;
        var dimensions = def.Dimensions?.ToList() ?? new List<string>();
        var metrics = def.Metrics?.ToList() ?? new List<string>();
        if (isLegacy && dimensions.Count == 0) dimensions.Add("date");
        if (metrics.Count == 0) metrics.Add("count");

        var columns = new List<ReportResultColumn>();
        foreach (var d in dimensions)
            columns.Add(new ReportResultColumn { Key = d, Label = schema.Label(d), DataType = DimensionExpressionBuilder.IsDateKey(d) ? "date" : "string" });
        foreach (var m in metrics)
            columns.Add(new ReportResultColumn { Key = m, Label = schema.Label(m), DataType = "number" });

        var rows = new List<Dictionary<string, object?>>();
        long matched = 0;
        var executionStatus = "succeeded";
        var startedAt = DateTime.UtcNow;
        var isTruncated = false;

        try
        {
            var col = _db.GetCollection<BsonDocument>(schema.CollectionName);
            var match = schema.BuildBaseMatch(report.TenantId, range);
            matched = await col.CountDocumentsAsync(match, cancellationToken: ct);

            var groupId = BuildGroupId(dimensions, schema);
            var group = new BsonDocument { { "_id", groupId } };

            // Ratio metrics are computed post-group from two base counts; make sure both
            // bases are accumulated (even if the user only asked for the ratio itself).
            var effectiveMetrics = metrics.ToList();
            foreach (var m in metrics)
            {
                if (!RatioMetrics.TryGetValue(m, out var r)) continue;
                if (!HasMetric(effectiveMetrics, r.Num)) effectiveMetrics.Add(r.Num);
                if (!HasMetric(effectiveMetrics, r.Den)) effectiveMetrics.Add(r.Den);
            }

            var accumulators = new List<(string Key, BsonDocument Acc)>();
            foreach (var m in effectiveMetrics)
            {
                var acc = MetricAccumulatorBuilder.Build(m, schema);
                if (acc is not null) accumulators.Add((m, acc));
            }

            // Guarantee at least one accumulator — otherwise Mongo returns just { _id }.
            if (accumulators.Count == 0)
            {
                accumulators.Add(("count", new BsonDocument("$sum", 1)));
                if (!HasMetric(metrics, "count")) metrics.Add("count");
            }
            foreach (var (k, a) in accumulators) group[k] = a;

            var pipeline = new List<BsonDocument>
            {
                new("$match", match),
                new("$group", group),
            };

            // Swap id dimensions (e.g. agent = userId) for their display name.
            pipeline.AddRange(BuildDisplayLookups(dimensions, schema));

            if (metrics.Any(RatioMetrics.ContainsKey))
                pipeline.Add(BuildRatioProjection(metrics));

            pipeline.Add(new BsonDocument("$sort", new BsonDocument("_id", 1)));
            pipeline.Add(new BsonDocument("$limit", MaxGroupResults + 1));

            _logger?.LogDebug("Report {ReportId} tenant {TenantId} pipeline: {Pipeline}",
                report.Id, report.TenantId, string.Join(" | ", pipeline.Select(p => p.ToJson())));

            using var cursor = await col.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
            await cursor.ForEachAsync(doc =>
            {
                if (rows.Count >= MaxGroupResults) { isTruncated = true; return; }
                rows.Add(MapRow(doc, dimensions, metrics));
            }, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MongoException ex)
        {
            _logger?.LogError(ex, "Metric report {ReportId} tenant {TenantId} failed", report.Id, report.TenantId);
            executionStatus = "failed";
        }

        var summary = new Dictionary<string, object?>
        {
            ["mode"] = "metricAndDimension",
            ["dataSource"] = dataSource,
            ["from"] = range.from.ToString("O"),
            ["to"] = range.to.ToString("O"),
            ["matched"] = matched,
            ["dimensionCount"] = dimensions.Count,
            ["metricCount"] = metrics.Count,
            ["groupCount"] = rows.Count,
            ["limit"] = MaxGroupResults,
            ["isTruncated"] = isTruncated,
            ["visualization"] = def.Visualization.ToString().ToLowerInvariant(),
            ["executionStatus"] = executionStatus,
            ["executionDurationMs"] = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds,
        };
        return new ReportExecutionOutput(columns, rows, summary);
    }

    /// <summary>
    /// For each dimension the catalog marks as a reference (id → name), append a post-group
    /// <c>$lookup</c> that replaces the grouped id with the looked-up display name. Runs on the
    /// grouped rows only, so it stays cheap. Falls back to the id when no match is found.
    /// </summary>
    private static IEnumerable<BsonDocument> BuildDisplayLookups(IReadOnlyList<string> dimensions, ReportSchema schema)
    {
        foreach (var d in dimensions)
        {
            var field = schema.FindField(d);
            if (field is null || !field.HasDisplayLookup) continue;

            var alias = $"__lk_{d}";
            var idPath = $"_id.{d}";

            // When joining on the target's _id (an ObjectId) the grouped value is a string,
            // so a plain localField/foreignField match never hits — use a $toString pipeline.
            if (string.Equals(field.LookupForeignField, "_id", StringComparison.OrdinalIgnoreCase))
            {
                yield return new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", field.LookupCollection },
                    { "let", new BsonDocument("id", $"${idPath}") },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument("$expr",
                                new BsonDocument("$eq", new BsonArray
                                {
                                    new BsonDocument("$toString", "$_id"), "$$id",
                                }))),
                        }
                    },
                    { "as", alias },
                });
            }
            else
            {
                yield return new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", field.LookupCollection },
                    { "localField", idPath },
                    { "foreignField", field.LookupForeignField },
                    { "as", alias },
                });
            }
            yield return new BsonDocument("$set", new BsonDocument(idPath,
                new BsonDocument("$ifNull", new BsonArray
                {
                    new BsonDocument("$arrayElemAt", new BsonArray { $"${alias}.{field.LookupDisplayField}", 0 }),
                    $"${idPath}",
                })));
            yield return new BsonDocument("$unset", alias);
        }
    }

    private static BsonValue BuildGroupId(IReadOnlyList<string> dimensions, ReportSchema schema)
    {
        if (dimensions.Count == 0) return BsonNull.Value;

        var idDoc = new BsonDocument();
        foreach (var dim in dimensions)
            idDoc[dim] = DimensionExpressionBuilder.Build(dim, schema);
        return idDoc;
    }

    /// <summary>
    /// Percentage metrics computed after grouping as <c>numerator / denominator * 100</c>.
    /// Both bases are plain count accumulators the catalog already knows how to build, so
    /// adding a ratio is just one entry here plus the two base metrics in the source.
    /// </summary>
    private static readonly Dictionary<string, (string Num, string Den)> RatioMetrics =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["answer_rate"] = ("answered_count", "count"),
            ["abandonment_rate"] = ("abandoned_count", "count"),
            ["connection_rate"] = ("connected_count", "count"),
            ["right_party_contact_rate"] = ("rpc_count", "count"),
            ["ob_conv"] = ("success_count", "count"),
            ["repeat_contact_rate"] = ("repeat_count", "count"),
            ["list_penetration"] = ("contacted_count", "count"),
            // Calls: answered within SLA / calls offered.
            ["service_level"] = ("answered_within_sla_count", "count"),
            ["recording_coverage"] = ("recorded_count", "count"),
            ["callback_rate"] = ("callback_count", "count"),
            ["hold_rate"] = ("held_count", "count"),
            ["negative_sentiment_rate"] = ("negative_sentiment_count", "count"),
            // Outbound: abandoned / answered (connected). Safe basis (abandoned ⊆ connected)
            // so the ratio never exceeds 100%; see catalog note on strict TCPA live-answer basis.
            ["abandonment_rate_tcpa"] = ("abandoned_count", "connected_count"),
        };

    private static BsonDocument BuildRatioProjection(IReadOnlyList<string> metrics)
    {
        var proj = new BsonDocument { { "_id", 1 } };
        foreach (var m in metrics)
        {
            if (RatioMetrics.TryGetValue(m, out var r))
            {
                proj[m] = new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$gt", new BsonArray { $"${r.Den}", 0 }),
                    new BsonDocument("$multiply", new BsonArray
                    {
                        new BsonDocument("$divide", new BsonArray { $"${r.Num}", $"${r.Den}" }),
                        100,
                    }),
                    0,
                });
            }
            else
            {
                proj[m] = 1;
            }
        }
        return new BsonDocument("$project", proj);
    }

    private static Dictionary<string, object?> MapRow(
        BsonDocument doc, IReadOnlyList<string> dimensions, IReadOnlyList<string> metrics)
    {
        var row = new Dictionary<string, object?>();
        var idVal = doc.GetValue("_id", BsonNull.Value);

        if (dimensions.Count == 0)
        {
            // no dimensions → single row of overall metrics
        }
        else if (idVal is BsonDocument idDoc)
        {
            foreach (var dim in dimensions)
                row[dim] = BsonValueConverter.ToClr(idDoc.GetValue(dim, BsonNull.Value));
        }
        else
        {
            row[dimensions[0]] = BsonValueConverter.ToClr(idVal);
        }

        foreach (var m in metrics)
            row[m] = BsonValueConverter.ToClr(doc.GetValue(m, BsonNull.Value));

        return row;
    }

    private static bool HasMetric(IEnumerable<string> metrics, string key) =>
        metrics.Any(m => string.Equals(m, key, StringComparison.OrdinalIgnoreCase));
}
