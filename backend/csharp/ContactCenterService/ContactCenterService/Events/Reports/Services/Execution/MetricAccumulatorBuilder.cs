using MongoDB.Bson;
using VoiceFlow.Core.Reports.Catalog;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Translates a logical metric key into its MongoDB <c>$group</c> accumulator.
///
/// The old executor guessed at accumulators by string-matching the metric key
/// (<c>"total_calls"</c> → count, <c>"avg_x"</c> → $avg, …), which drifted from what
/// the frontend actually sends. Mapping now flows through
/// <see cref="ReportDataSourceCatalog"/>: the catalog owns each metric's
/// <see cref="ReportMetricKind"/>, and this builder owns the Mongo expression for that
/// kind. The frontend and backend therefore agree on one source of truth.
///
/// Returns <c>null</c> for metrics that are computed after grouping
/// (<see cref="ReportMetricKind.AnswerRate"/>) — those are emitted via a <c>$project</c>
/// stage by the caller.
/// </summary>
internal static class MetricAccumulatorBuilder
{
    private static readonly string[] AnsweredStatuses = { "Completed" };
    private static readonly BsonArray UnansweredStatuses =
        new() { "NoAnswer", "Busy", "Failed", "Rejected", "Missed" };
    // Outbound dispositions the campaign engine treats as a successful contact
    // (see Outbound DispositionMapper.ToTerminal).
    private static readonly BsonArray SuccessDispositions =
        new() { "answered", "delivered", "sale", "success" };
    // Campaign target statuses that mean "not yet dialed"; anything else counts as contacted.
    private static readonly BsonArray NotContactedStatuses =
        new() { "pending", "new", "queued", "" };
    // Sentiment is stored as an int enum (Negative == 2) but an AI worker may write a string;
    // match both so the metric is robust to either representation.
    private static readonly BsonArray NegativeSentiments = new() { 2, "Negative", "negative" };

    // Service-level threshold: a call answered within this many seconds of ringing counts
    // toward Service Level. 20s is the industry-standard default (the "80/20" target).
    private const int SlaThresholdSeconds = 20;

    public static BsonDocument? Build(string metricKey, ReportSchema schema)
    {
        var kind = schema.FindMetric(metricKey)?.Kind;

        // Unknown to the catalog: keep the legacy avg_/sum_/min_/max_<field> prefix
        // support so custom metric keys still resolve to a real field.
        if (kind is null)
            return BuildFromPrefix(metricKey, schema);

        return kind switch
        {
            ReportMetricKind.Count => new BsonDocument("$sum", 1),
            ReportMetricKind.SumDuration => SumOf(schema.DurationField),
            ReportMetricKind.AvgDuration => new BsonDocument("$avg", $"${schema.DurationField}"),
            ReportMetricKind.MinDuration => new BsonDocument("$min", $"${schema.DurationField}"),
            ReportMetricKind.MaxDuration => new BsonDocument("$max", $"${schema.DurationField}"),
            ReportMetricKind.HoldTime => SumOf(schema.HoldField),
            ReportMetricKind.AnsweredCount => AnsweredCount(),
            ReportMetricKind.UnansweredCount => CountWhereStatusIn(UnansweredStatuses),
            ReportMetricKind.AbandonedCount => AbandonedCount(),
            ReportMetricKind.InboundCount => CountWhereDirection("inbound"),
            ReportMetricKind.OutboundCount => CountWhereDirection("outbound"),
            ReportMetricKind.InternalCount => CountWhereDirection("internal"),

            // Calls — wrap-up derived.
            ReportMetricKind.VoicemailCount => CountWhereEq("$status", "Voicemail"),
            ReportMetricKind.SumAcw => SumOf("wrapUp.acwSeconds"),
            // AHT = talk/total handle + after-call work.
            ReportMetricKind.AvgHandleTime => new BsonDocument("$avg",
                new BsonDocument("$add", new BsonArray
                {
                    new BsonDocument("$ifNull", new BsonArray { $"${schema.DurationField}", 0 }),
                    new BsonDocument("$ifNull", new BsonArray { "$wrapUp.acwSeconds", 0 }),
                })),

            // Calls — speed of answer / service level (from ringSeconds + answeredAt).
            ReportMetricKind.AvgSpeedOfAnswer => AvgSpeedOfAnswer(schema),
            ReportMetricKind.AnsweredWithinSlaCount => AnsweredWithinSla(schema),
            ReportMetricKind.LongestWait => new BsonDocument("$max", $"${schema.MapField("ringSeconds")}"),
            ReportMetricKind.RecordedCount => CountWhereEq("$hasRecording", true),
            ReportMetricKind.CallbackCount => CountWhereEq("$wrapUp.callbackScheduled", true),
            ReportMetricKind.HeldCount => CountWhereGt($"${schema.HoldField}", 0),
            ReportMetricKind.NegativeSentimentCount => CountWhereIn("$sentiment", NegativeSentiments),

            // Outbound attempts.
            ReportMetricKind.ConnectedCount => CountWhereEq("$dialStatus", "ANSWER"),
            ReportMetricKind.RightPartyCount => CountWhereEq("$amdStatus", "HUMAN"),
            ReportMetricKind.AnsweringMachineCount => CountWhereEq("$amdStatus", "MACHINE"),
            ReportMetricKind.AbandonedDispositionCount => CountWhereEq("$disposition", "abandoned"),
            ReportMetricKind.AvgAttemptsToContact => AvgAttemptsToContact(),
            ReportMetricKind.SuccessCount => CountWhereIn("$disposition", SuccessDispositions),
            ReportMetricKind.AvgWaitTime => new BsonDocument("$avg", "$queueWaitSec"),

            // Campaign targets — "contacted" = any status past the initial not-yet-dialed states.
            ReportMetricKind.ContactedCount => CountWhereNotIn("$status", NotContactedStatuses),
            // Customers — a repeat customer has been on more than one call.
            ReportMetricKind.RepeatContactCount => CountWhereGt("$totalCalls", 1),

            // Computed post-group (ratios) — see MetricReportBuilder.RatioMetrics.
            ReportMetricKind.AnswerRate
                or ReportMetricKind.AbandonmentRate
                or ReportMetricKind.ConnectionRate
                or ReportMetricKind.RightPartyRate
                or ReportMetricKind.ConversionRate
                or ReportMetricKind.SuccessRate
                or ReportMetricKind.ServiceLevel
                or ReportMetricKind.PostGroupRatio
                or ReportMetricKind.ContactRate => null,
            
            // Extended kinds (agents/queues/tickets/…) aren't implemented yet; the
            // catalog documents that these fall back to a plain count until their
            // data source gets first-class support.
            _ => new BsonDocument("$sum", 1),
        };
    }

    private static BsonDocument SumOf(string field) =>
        new("$sum", new BsonDocument("$ifNull", new BsonArray { $"${field}", 0 }));

    // A call counts as answered when its status is Completed or it has an answeredAt timestamp.
    private static BsonDocument IsAnswered() =>
        new("$or", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { "$status", AnsweredStatuses[0] }),
            new BsonDocument("$ne", new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$answeredAt", BsonNull.Value }),
                BsonNull.Value,
            }),
        });

    private static BsonDocument AnsweredCount() =>
        new("$sum", new BsonDocument("$cond", new BsonArray { IsAnswered(), 1, 0 }));

    // ASA — average seconds a caller waited before being answered. Averages the ring time of
    // answered calls only; $avg ignores the null the $cond yields for unanswered calls.
    private static BsonDocument AvgSpeedOfAnswer(ReportSchema schema)
    {
        var ring = $"${schema.MapField("ringSeconds")}";
        return new BsonDocument("$avg", new BsonDocument("$cond", new BsonArray
        {
            IsAnswered(),
            new BsonDocument("$ifNull", new BsonArray { ring, BsonNull.Value }),
            BsonNull.Value,
        }));
    }

    // Avg attempts to contact — the attempt number at which a connect happened, averaged over
    // connected attempts. $avg ignores the null yielded for non-connecting dials, so the result
    // is "on average, contact was made on the Nth attempt".
    private static BsonDocument AvgAttemptsToContact() =>
        new("$avg", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { "$dialStatus", "ANSWER" }),
            "$attemptNumber",
            BsonNull.Value,
        }));

    // Service-level numerator: answered AND ring time within the SLA threshold.
    private static BsonDocument AnsweredWithinSla(ReportSchema schema)
    {
        var ring = new BsonDocument("$ifNull",
            new BsonArray { $"${schema.MapField("ringSeconds")}", int.MaxValue });
        return new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$and", new BsonArray
            {
                IsAnswered(),
                new BsonDocument("$lte", new BsonArray { ring, SlaThresholdSeconds }),
            }),
            1, 0,
        }));
    }

    private static BsonDocument AbandonedCount() =>
        new("$sum", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray { "$status", "Missed" }),
                new BsonDocument("$eq", new BsonArray { "$hangupCause", "Abandoned" }),
            }),
            1, 0,
        }));

    private static BsonDocument CountWhereStatusIn(BsonArray statuses) =>
        CountWhereIn("$status", statuses);

    private static BsonDocument CountWhereEq(string field, BsonValue value) =>
        new("$sum", new BsonDocument("$cond",
            new BsonArray { new BsonDocument("$eq", new BsonArray { field, value }), 1, 0 }));

    private static BsonDocument CountWhereIn(string field, BsonArray values) =>
        new("$sum", new BsonDocument("$cond",
            new BsonArray { new BsonDocument("$in", new BsonArray { field, values }), 1, 0 }));

    private static BsonDocument CountWhereNotIn(string field, BsonArray values) =>
        new("$sum", new BsonDocument("$cond",
            new BsonArray { new BsonDocument("$not", new BsonDocument("$in", new BsonArray { field, values })), 1, 0 }));

    private static BsonDocument CountWhereGt(string field, BsonValue value) =>
        new("$sum", new BsonDocument("$cond",
            new BsonArray { new BsonDocument("$gt", new BsonArray { new BsonDocument("$ifNull", new BsonArray { field, 0 }), value }), 1, 0 }));

    private static BsonDocument CountWhereDirection(string direction) =>
        new("$sum", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$toLower", new BsonDocument("$ifNull", new BsonArray { "$direction", string.Empty })),
                direction,
            }),
            1, 0,
        }));

    private static BsonDocument? BuildFromPrefix(string metricKey, ReportSchema schema)
    {
        var idx = metricKey.IndexOf('_');
        if (idx <= 0) return null;

        var prefix = metricKey[..idx].ToLowerInvariant();
        var field = schema.MapField(metricKey[(idx + 1)..]);
        var expr = new BsonDocument("$ifNull", new BsonArray { $"${field}", 0 });
        return prefix switch
        {
            "avg" => new BsonDocument("$avg", expr),
            "sum" => new BsonDocument("$sum", expr),
            "min" => new BsonDocument("$min", expr),
            "max" => new BsonDocument("$max", expr),
            _ => null,
        };
    }
}
