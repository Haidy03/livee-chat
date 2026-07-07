using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Reports.Catalog;
using VoiceFlow.Infrastructure.Persistence;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Per-campaign rollup: one row per campaign joining <c>campaigns</c> (status),
/// <c>campaign_targets</c> (list outcomes) and <c>call_attempts</c> (dial activity). The two
/// joins are <b>pre-aggregated</b> inside the <c>$lookup</c> sub-pipeline (they return a single
/// counts document, not the raw rows), so it stays efficient even for large lists.
/// Lifetime totals — the report date range is not applied (a campaign's list/attempts are cumulative).
/// </summary>
internal sealed class CampaignSummaryBuilder
{
    private const int MaxRows = 1000;

    private static readonly string[] AllMetrics =
    {
        "targets", "contacted", "succeeded", "failed", "list_penetration", "success_rate",
        "attempts", "connected", "connection_rate", "rpc", "rpc_rate",
        "machine", "abandoned", "abandonment_rate",
        "agents", "avg_duration", "avg_queue_wait",
    };

    // Target statuses that mean "not yet dialed"; anything else counts as contacted.
    private static readonly BsonArray NotContacted = new() { "pending", "new", "queued", "" };

    private readonly MongoDbContext _db;
    private readonly ILogger? _logger;

    public CampaignSummaryBuilder(MongoDbContext db, ILogger? logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReportExecutionOutput> BuildAsync(
        Report report, string dataSource, (DateTimeOffset from, DateTimeOffset to) range, CancellationToken ct)
    {
        var def = report.Definition;
        var requested = (def.Metrics ?? new List<string>())
            .Where(m => AllMetrics.Contains(m, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count == 0) requested = AllMetrics.ToList();

        var catalog = ReportDataSourceCatalog.Find("campaigns");
        string Label(string key) => catalog?.FindMetric(key)?.LabelEn ?? key;
        string TypeOf(string key) => catalog?.FindMetric(key)?.DataType ?? "number";

        var columns = new List<ReportResultColumn>
        {
            new() { Key = "campaign", Label = "Campaign", DataType = "string" },
            new() { Key = "status", Label = "Status", DataType = "string" },
        };
        columns.AddRange(requested.Select(m => new ReportResultColumn { Key = m, Label = Label(m), DataType = TypeOf(m) }));

        var rows = new List<Dictionary<string, object?>>();
        long matched = 0;
        var executionStatus = "succeeded";
        var startedAt = DateTime.UtcNow;

        try
        {
            var campaigns = _db.GetCollection<BsonDocument>("campaigns");
            matched = await campaigns.CountDocumentsAsync(new BsonDocument("tenantId", report.TenantId), cancellationToken: ct);

            using var cursor = await campaigns.AggregateAsync<BsonDocument>(BuildPipeline(report.TenantId), cancellationToken: ct);
            await cursor.ForEachAsync(doc =>
            {
                if (rows.Count >= MaxRows) return;
                var row = new Dictionary<string, object?>
                {
                    ["campaign"] = BsonValueConverter.ToClr(doc.GetValue("campaign", BsonNull.Value)),
                    ["status"] = BsonValueConverter.ToClr(doc.GetValue("status", BsonNull.Value)),
                };
                foreach (var m in requested)
                    row[m] = BsonValueConverter.ToClr(doc.GetValue(m, BsonNull.Value));
                rows.Add(row);
            }, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MongoException ex)
        {
            _logger?.LogError(ex, "Campaign summary {ReportId} tenant {TenantId} failed", report.Id, report.TenantId);
            executionStatus = "failed";
        }

        var summary = new Dictionary<string, object?>
        {
            ["mode"] = "campaignSummary",
            ["dataSource"] = dataSource,
            ["matched"] = matched,
            ["groupCount"] = rows.Count,
            ["executionStatus"] = executionStatus,
            ["executionDurationMs"] = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds,
        };
        return new ReportExecutionOutput(columns, rows, summary);
    }

    private static List<BsonDocument> BuildPipeline(string tenantId)
    {
        // campaignId on the child collections is a string; the campaign _id is an ObjectId.
        BsonDocument ChildLookup(string from, BsonDocument group) => new("$lookup", new BsonDocument
        {
            { "from", from },
            { "let", new BsonDocument("cid", new BsonDocument("$toString", "$_id")) },
            { "pipeline", new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument("$expr",
                        new BsonDocument("$eq", new BsonArray { "$campaignId", "$$cid" }))),
                    group,
                }
            },
        });

        BsonDocument Sum(BsonValue expr) => new("$sum", expr);
        BsonDocument CountIf(BsonValue cond) => Sum(new BsonDocument("$cond", new BsonArray { cond, 1, 0 }));
        BsonDocument Eq(string field, BsonValue v) => new("$eq", new BsonArray { field, v });

        var targets = ChildLookup("campaign_targets", new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "total", Sum(1) },
            // contacted = anything past the not-yet-dialed statuses (swap the $cond branches).
            { "contacted", Sum(new BsonDocument("$cond", new BsonArray
                { new BsonDocument("$in", new BsonArray { "$status", NotContacted }), 0, 1 })) },
            { "succeeded", CountIf(Eq("$status", "successful")) },
            { "failed", CountIf(Eq("$status", "failed")) },
        }));
        targets["$lookup"].AsBsonDocument["as"] = "t";

        var attempts = ChildLookup("call_attempts", new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "attempts", Sum(1) },
            { "connected", CountIf(Eq("$dialStatus", "ANSWER")) },
            { "rpc", CountIf(Eq("$amdStatus", "HUMAN")) },
            { "machine", CountIf(Eq("$amdStatus", "MACHINE")) },
            { "abandoned", CountIf(Eq("$disposition", "abandoned")) },
            { "agents", new BsonDocument("$addToSet", "$agentId") },
            { "durSum", Sum(new BsonDocument("$ifNull", new BsonArray { "$durationSec", 0 })) },
            { "durCount", CountIf(new BsonDocument("$gt", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$durationSec", 0 }), 0 })) },
            { "waitSum", Sum(new BsonDocument("$ifNull", new BsonArray { "$queueWaitSec", 0 })) },
            { "waitCount", CountIf(new BsonDocument("$gt", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$queueWaitSec", 0 }), 0 })) },
        }));
        attempts["$lookup"].AsBsonDocument["as"] = "a";

        var addFields = new BsonDocument("$addFields", new BsonDocument
        {
            { "_t", new BsonDocument("$ifNull", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$t", 0 }), new BsonDocument() }) },
            { "_a", new BsonDocument("$ifNull", new BsonArray { new BsonDocument("$arrayElemAt", new BsonArray { "$a", 0 }), new BsonDocument() }) },
        });

        BsonValue T(string f) => new BsonDocument("$ifNull", new BsonArray { $"$_t.{f}", 0 });
        BsonValue A(string f) => new BsonDocument("$ifNull", new BsonArray { $"$_a.{f}", 0 });
        BsonValue Pct(BsonValue num, BsonValue den) => new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$gt", new BsonArray { den, 0 }),
            new BsonDocument("$multiply", new BsonArray { new BsonDocument("$divide", new BsonArray { num, den }), 100 }),
            0,
        });
        BsonValue Avg(string sum, string count) => new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$gt", new BsonArray { A(count), 0 }),
            new BsonDocument("$divide", new BsonArray { A(sum), A(count) }),
            0,
        });

        var project = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 },
            { "campaign", new BsonDocument("$ifNull", new BsonArray { "$name", "$_id" }) },
            { "status", new BsonDocument("$ifNull", new BsonArray { "$status", "" }) },
            { "targets", T("total") },
            { "contacted", T("contacted") },
            { "succeeded", T("succeeded") },
            { "failed", T("failed") },
            { "attempts", A("attempts") },
            { "connected", A("connected") },
            { "agents", new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                {
                    { "input", new BsonDocument("$ifNull", new BsonArray { "$_a.agents", new BsonArray() }) },
                    { "as", "x" },
                    { "cond", new BsonDocument("$and", new BsonArray
                        {
                            new BsonDocument("$ne", new BsonArray { "$$x", BsonNull.Value }),
                            new BsonDocument("$ne", new BsonArray { "$$x", "" }),
                        }) },
                })) },
            { "rpc", A("rpc") },
            { "rpc_rate", Pct(A("rpc"), A("attempts")) },
            { "machine", A("machine") },
            { "abandoned", A("abandoned") },
            { "avg_duration", Avg("durSum", "durCount") },
            { "avg_queue_wait", Avg("waitSum", "waitCount") },
            { "list_penetration", Pct(T("contacted"), T("total")) },
            { "success_rate", Pct(T("succeeded"), T("total")) },
            { "connection_rate", Pct(A("connected"), A("attempts")) },
            // Abandoned over answered (connected) — mirrors the outbound source's basis.
            { "abandonment_rate", Pct(A("abandoned"), A("connected")) },
        });

        return new List<BsonDocument>
        {
            new("$match", new BsonDocument("tenantId", tenantId)),
            targets,
            attempts,
            addFields,
            project,
            new("$sort", new BsonDocument("campaign", 1)),
            new("$limit", MaxRows + 1),
        };
    }
}
