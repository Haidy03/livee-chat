using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Core.Entities.Reports;

[BsonIgnoreExtraElements]
public sealed class Schedule
{
    [BsonRepresentation(BsonType.String)]
    [BsonElement("frequency")]
    public ScheduleFrequency Frequency { get; set; } = ScheduleFrequency.Daily;
    [BsonElement("runTime")]
    public string RunTime { get; set; } = "08:00";
    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("weekDays")]

    public List<string> WeekDays { get; set; } = new();
    [BsonElement("monthDays")]
    public List<int> MonthDays { get; set; } = new();

    [BsonElement("cron")]
    public string? Cron { get; set; }
    [BsonElement("recipients")]
    public List<string> Recipients { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    [BsonElement("formats")]
    public List<ExportFormat> Formats { get; set; } = new();

    [BsonElement("slack")]
    public bool Slack { get; set; }

    [BsonElement("webhook")]
    public bool Webhook { get; set; }

    [BsonElement("sftp")]
    public bool Sftp { get; set; }

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;
}
