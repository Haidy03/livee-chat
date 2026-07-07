using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Infrastructure.Persistence;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Executes a <see cref="ReportMode.Detail"/> report: one row per matching document,
/// projected down to the user-selected fields, sorted and paginated.
/// </summary>
internal sealed class DetailReportBuilder
{
    private const int MaxPageSize = 500;
    private const int DefaultPageSize = 50;

    private readonly MongoDbContext _db;
    private readonly ILogger? _logger;

    public DetailReportBuilder(MongoDbContext db, ILogger? logger)
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
        var selected = (def.SelectedFields ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith('$'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var columns = selected
            .Select(f => new ReportResultColumn { Key = f, Label = f, DataType = schema.TypeFor(f) })
            .ToList();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize is <= 0 or > MaxPageSize ? DefaultPageSize : request.PageSize;

        var rows = new List<Dictionary<string, object?>>();
        long matched = 0;
        var returned = 0;
        var sortField = def.Sort?.Field ?? schema.DateField;
        var sortDir = (def.Sort?.Direction ?? VoiceFlow.Core.Enums.Reports.SortDirection.Desc)
            == VoiceFlow.Core.Enums.Reports.SortDirection.Asc ? "asc" : "desc";
        var executionStatus = "succeeded";

        if (selected.Count == 0)
        {
            return new ReportExecutionOutput(columns, rows, BuildSummary(
                dataSource, range, matched, returned, page, pageSize, sortField, sortDir, "no_fields_selected"));
        }

        var startedAt = DateTime.UtcNow;
        try
        {
            var col = _db.GetCollection<BsonDocument>(schema.CollectionName);
            var match = schema.BuildBaseMatch(report.TenantId, range);
            
            matched = await col.CountDocumentsAsync(match, cancellationToken: ct);

            var projection = new BsonDocument { { "_id", 0 } };
            foreach (var f in selected)
                projection[schema.MapField(f)] = 1;

            var sortDoc = new BsonDocument
            {
                { schema.MapField(sortField), sortDir == "asc" ? 1 : -1 },
                { "_id", -1 }, // stable tiebreaker
            };

            var find = col.Find(match)
                .Project<BsonDocument>(projection)
                .Sort(sortDoc)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize);

            using var cursor = await find.ToCursorAsync(ct);
            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var doc in cursor.Current)
                {
                    var row = new Dictionary<string, object?>();
                    foreach (var f in selected)
                        row[f] = BsonValueConverter.ToDisplay(doc.GetValue(schema.MapField(f), BsonNull.Value));
                    rows.Add(row);
                    returned++;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MongoException ex)
        {
            _logger?.LogError(ex, "Detail report {ReportId} tenant {TenantId} failed", report.Id, report.TenantId);
            executionStatus = "failed";
        }

        var summary = BuildSummary(dataSource, range, matched, returned, page, pageSize, sortField, sortDir, executionStatus);
        summary["executionDurationMs"] = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        return new ReportExecutionOutput(columns, rows, summary);
    }

    private static Dictionary<string, object?> BuildSummary(
        string dataSource,
        (DateTimeOffset from, DateTimeOffset to) range,
        long matched, int returned, int page, int pageSize,
        string sortField, string sortDir, string executionStatus)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(matched / (double)pageSize) : 0;
        return new Dictionary<string, object?>
        {
            ["mode"] = "detail",
            ["dataSource"] = dataSource,
            ["from"] = range.from.ToString("O"),
            ["to"] = range.to.ToString("O"),
            ["matched"] = matched,
            ["returned"] = returned,
            ["page"] = page,
            ["pageSize"] = pageSize,
            ["totalPages"] = totalPages,
            ["sortField"] = sortField,
            ["sortDirection"] = sortDir,
            ["executionStatus"] = executionStatus,
        };
    }
}
