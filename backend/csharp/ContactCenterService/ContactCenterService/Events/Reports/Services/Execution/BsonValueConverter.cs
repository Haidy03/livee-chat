using MongoDB.Bson;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>Converts BSON values pulled from a report query into plain CLR objects for serialization.</summary>
internal static class BsonValueConverter
{
    // Keys tried, in order, when collapsing an embedded document to a single label.
    private static readonly string[] LabelKeys = { "name", "label", "displayName", "title", "value", "code" };

    /// <summary>
    /// Display value for a flat detail cell. Scalars keep their CLR type (numbers/dates stay typed);
    /// arrays and embedded documents are collapsed to a readable string — e.g. the skills array
    /// <c>[{name:"English",…},{name:"Arabic",…}]</c> becomes <c>"English, Arabic"</c> — so the cell
    /// isn't raw JSON.
    /// </summary>
    public static object? ToDisplay(BsonValue v)
    {
        if (v is null || v.IsBsonNull || v.BsonType == BsonType.Undefined) return null;
        return v.BsonType switch
        {
            BsonType.Array => JoinItems(v.AsBsonArray),
            BsonType.Document => Summarize(v),
            _ => ToClr(v),
        };
    }

    private static object? JoinItems(BsonArray arr)
    {
        var joined = string.Join(", ", arr.Select(Summarize).Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    private static string Summarize(BsonValue v)
    {
        if (v is null || v.IsBsonNull || v.BsonType == BsonType.Undefined) return string.Empty;
        if (v.BsonType == BsonType.Array)
            return string.Join(", ", v.AsBsonArray.Select(Summarize).Where(s => !string.IsNullOrWhiteSpace(s)));
        if (v.BsonType == BsonType.Document)
        {
            var d = v.AsBsonDocument;
            foreach (var key in LabelKeys)
                if (d.TryGetValue(key, out var label) && !label.IsBsonNull)
                    return Summarize(label);
            return d.ToJson(); // no recognizable label — fall back to raw
        }
        return ToClr(v)?.ToString() ?? string.Empty;
    }

    public static object? ToClr(BsonValue v)
    {
        if (v is null || v.IsBsonNull || v.BsonType == BsonType.Undefined) return null;
        return v.BsonType switch
        {
            BsonType.Int32 => v.ToInt32(),
            BsonType.Int64 => v.ToInt64(),
            BsonType.Double => v.ToDouble(),
            BsonType.Decimal128 => (decimal)v.AsDecimal128,
            BsonType.Boolean => v.AsBoolean,
            BsonType.DateTime => v.ToUniversalTime(),
            BsonType.String => v.AsString,
            BsonType.ObjectId => v.AsObjectId.ToString(),
            BsonType.Array => v.AsBsonArray.Select(ToClr).ToList(),
            BsonType.Document => v.AsBsonDocument.Elements.ToDictionary(x => x.Name, x => ToClr(x.Value)),
            _ => v.ToString(),
        };
    }
}
