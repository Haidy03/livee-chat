using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Entities.Reports;


[BsonIgnoreExtraElements]
public sealed class ReportFilters
{
    [BsonElement("dateRange")]
    public string DateRange { get; set; } = "last_30_days";

    [BsonElement("channels")]
    public string Channels { get; set; } = "all";

    [BsonElement("agents")]
    public List<string> Agents { get; set; } = new();

    [BsonElement("queues")]
    public List<string> Queues { get; set; } = new();

    [BsonElement("skills")]
    public List<string> Skills { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class ReportSortDefinition
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [BsonElement("direction")]
    public SortDirection Direction { get; set; } = SortDirection.Desc;
}

[BsonIgnoreExtraElements]
public sealed class ReportDefinition
{
    /// <summary>
    /// Execution mode. Legacy documents without this field are treated as
    /// <see cref="ReportMode.MetricAndDimension"/> for backward compatibility.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    [BsonElement("mode")]
    [BsonIgnoreIfDefault(false)]
    public ReportMode Mode { get; set; } = ReportMode.MetricAndDimension;

    [BsonElement("dataSource")]
    public string DataSource { get; set; } = string.Empty;

    /// <summary>Ordered list of field keys for Detail reports.</summary>
    [BsonElement("selectedFields")]
    public List<string> SelectedFields { get; set; } = new();

    [BsonElement("metrics")]
    public List<string> Metrics { get; set; } = new();

    [BsonElement("dimensions")]
    public List<string> Dimensions { get; set; } = new();

    [BsonElement("filters")]
    public ReportFilters Filters { get; set; } = new();

    [BsonElement("sort")]
    public ReportSortDefinition? Sort { get; set; }

    [BsonElement("visualization")]
    public VizId Visualization { get; set; } = VizId.Table;

    /// <summary>
    /// 1 = pre-mode legacy layout, 2 = new layout with explicit mode/selectedFields.
    /// Legacy docs deserialize as 0 which is treated as 1.
    /// </summary>
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; } = 2;
}
