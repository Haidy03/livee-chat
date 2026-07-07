

using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.Reports
{
    [BsonIgnoreExtraElements]
    public sealed class ReportResultColumn
    {
        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("dataType")]
        public string DataType { get; set; } = "string"; // string|number|date|boolean
    }

    [BsonIgnoreExtraElements]
    public sealed class ReportResult : Entity, ITenantScoped
    {
        [BsonElement("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("reportId")]
        public string ReportId { get; set; } = string.Empty;

        [BsonElement("runId")]
        public string RunId { get; set; } = string.Empty;

        [BsonElement("generatedAt")]
        public DateTimeOffset GeneratedAt { get; set; }

        [BsonElement("columns")]
        public List<ReportResultColumn> Columns { get; set; } = new();

        [BsonElement("rows")]
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        [BsonElement("summary")]
        public Dictionary<string, object?> Summary { get; set; } = new();

        [BsonElement("rowCount")]
        public int RowCount { get; set; }
    }

}
