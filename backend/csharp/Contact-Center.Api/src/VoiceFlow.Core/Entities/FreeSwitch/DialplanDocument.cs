using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.FreeSwitch
{
    [BsonIgnoreExtraElements]
    public sealed class DialplanDocument : Entity, ITenantScoped
    {
        [BsonElement("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("domain")]
        public string Domain { get; set; } = string.Empty;

        [BsonElement("context")]
        public string Context { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("enabled")]
        public bool Enabled { get; set; }

        [BsonElement("priority")]
        public int Priority { get; set; }

        [BsonElement("renderMode")]
        public string RenderMode { get; set; } = "structured";

        [BsonElement("entries")]
        public List<DialplanEntry> Entries { get; set; } = [];
    }

    public sealed class DialplanEntry
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("routeType")]
        public string RouteType { get; set; } = "default";

        [BsonElement("priority")]
        public int Priority { get; set; }

        [BsonElement("match")]
        public DialplanMatch Match { get; set; } = new();

        [BsonElement("validation")]
        public DialplanValidation? Validation { get; set; }

        [BsonElement("actions")]
        public List<DialplanAction> Actions { get; set; } = [];
    }

    public sealed class DialplanMatch
    {
        [BsonElement("field")]
        public string Field { get; set; } = "destination_number";

        [BsonElement("type")]
        public string Type { get; set; } = "regex";

        [BsonElement("pattern")]
        public string Pattern { get; set; } = string.Empty;
    }

    public sealed class DialplanValidation
    {
        [BsonElement("field")]
        public string Field { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("pattern")]
        public string Pattern { get; set; } = string.Empty;
    }

    public sealed class DialplanAction
    {
        [BsonElement("application")]
        public string Application { get; set; } = string.Empty;

        [BsonElement("data")]
        [BsonRepresentation(BsonType.String)]
        public string? Data { get; set; }
    }
}
