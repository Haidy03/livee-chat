using MongoDB.Bson;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Builds the <c>$group._id</c> expression for a single dimension. Plain dimensions
/// resolve to their physical Mongo field; date-bucket dimensions (<c>hour</c>,
/// <c>dayofweek</c>, <c>week</c>, <c>month</c>, <c>quarter</c>, <c>year</c>) resolve to a
/// derived expression over the source's date field.
/// </summary>
internal static class DimensionExpressionBuilder
{
    private static string Normalize(string key) => key.ToLowerInvariant().Replace("_", "");

    public static bool IsDateKey(string key) => Normalize(key) switch
    {
        "date" or "hour" or "dow" or "dayofweek" or "week" or "month" or "quarter" or "year" => true,
        _ => false,
    };

    public static BsonValue Build(string dimension, ReportSchema schema)
    {
        if (!IsDateKey(dimension))
        {
            var field = schema.MapField(dimension);
            // Sentiment is persisted as the enum's ordinal; map it back to a label so the
            // report groups on "Positive/Neutral/Negative" instead of 0/1/2. Passes through
            // unchanged if the value is already a string (forward-compatible).
            if (field.Equals("sentiment", StringComparison.OrdinalIgnoreCase))
                return SentimentLabel(field);
            return $"${field}";
        }

        var date = $"${schema.DateField}";

        // Day of week ($dayOfWeek is 1=Sun … 7=Sat) → weekday name.
        if (Normalize(dimension) is "dow" or "dayofweek")
            return new BsonDocument("$arrayElemAt", new BsonArray
            {
                new BsonArray { "", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                new BsonDocument("$dayOfWeek", date),
            });

        // Quarter → "YYYY-Qn".
        if (Normalize(dimension) == "quarter")
            return new BsonDocument("$concat", new BsonArray
            {
                new BsonDocument("$toString", new BsonDocument("$year", date)),
                "-Q",
                new BsonDocument("$toString", new BsonDocument("$toInt",
                    new BsonDocument("$ceil", new BsonDocument("$divide", new BsonArray
                    {
                        new BsonDocument("$month", date), 3,
                    })))),
            });

        var format = Normalize(dimension) switch
        {
            "hour" => "%Y-%m-%d %H:00",
            "week" => "%G-W%V",
            "month" => "%Y-%m",
            "year" => "%Y",
            _ => "%Y-%m-%d",
        };
        return new BsonDocument("$dateToString", new BsonDocument
        {
            { "format", format },
            { "date", date },
        });
    }

    private static BsonValue SentimentLabel(string field) =>
        new BsonDocument("$switch", new BsonDocument
        {
            { "branches", new BsonArray
                {
                    new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { $"${field}", 0 }) }, { "then", "Positive" } },
                    new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { $"${field}", 1 }) }, { "then", "Neutral" } },
                    new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { $"${field}", 2 }) }, { "then", "Negative" } },
                }
            },
            { "default", $"${field}" },
        });
}
