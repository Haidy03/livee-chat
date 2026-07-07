using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Queue : Entity, ITenantScoped
{
    [BsonElement("tenantId")] public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")] public string Name { get; set; } = string.Empty;
    [BsonElement("code")] public string Code { get; set; } = string.Empty;
    [BsonElement("description")] public string? Description { get; set; }
    [BsonElement("channel")] public string Channel { get; set; } = "voice";
    [BsonElement("departmentId")] public string? DepartmentId { get; set; }
    [BsonElement("status")] public string Status { get; set; } = "draft";
    [BsonElement("tags")] public List<string> Tags { get; set; } = [];

    [BsonElement("routingMode")] public string RoutingMode { get; set; } = "static";
    [BsonElement("routingStrategy")] public string RoutingStrategy { get; set; } = "ringall";
    [BsonElement("tieBreaker")] public string TieBreaker { get; set; } = "longest_idle";
    [BsonElement("skillMatchLogic")] public string SkillMatchLogic { get; set; } = "all";

    [BsonElement("requiredSkills")] public List<QueueSkillRequirement> RequiredSkills { get; set; } = [];
    [BsonElement("members")] public List<QueueMember> Members { get; set; } = [];
    [BsonElement("fallbackAgents")] public List<QueueFallbackAgent> FallbackAgents { get; set; } = [];

    [BsonElement("skillRelaxationEnabled")] public bool SkillRelaxationEnabled { get; set; }
    [BsonElement("skillRelaxationRules")] public List<QueueSkillRelaxationRule> SkillRelaxationRules { get; set; } = [];

    [BsonElement("settings")] public QueueSettings Settings { get; set; } = new();
    [BsonElement("overflowRules")] public List<QueueOverflowRule> OverflowRules { get; set; } = [];
    [BsonElement("workingHours")] public QueueWorkingHours WorkingHours { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class QueueSkillRequirement
{
    [BsonElement("id")] public string Id { get; set; } = string.Empty;
    [BsonElement("skillId")] public string SkillId { get; set; } = string.Empty;
    [BsonElement("minimumLevel")] public int MinimumLevel { get; set; }
    [BsonElement("requirementType")] public string RequirementType { get; set; } = "mandatory";
    [BsonElement("weight")] public int Weight { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueMember
{
    [BsonElement("id")] public string Id { get; set; } = string.Empty;
    [BsonElement("agentId")] public string AgentId { get; set; } = string.Empty;
    [BsonElement("priority")] public int Priority { get; set; }
    [BsonElement("penalty")] public int Penalty { get; set; }
    [BsonElement("enabled")] public bool Enabled { get; set; } = true;
    [BsonElement("maxConcurrentInteractions")] public int? MaxConcurrentInteractions { get; set; }
    [BsonElement("wrapUpTimeOverride")] public int? WrapUpTimeOverride { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueFallbackAgent
{
    [BsonElement("id")] public string Id { get; set; } = string.Empty;
    [BsonElement("agentId")] public string AgentId { get; set; } = string.Empty;
    [BsonElement("priority")] public int Priority { get; set; }
    [BsonElement("triggerAfterSeconds")] public int TriggerAfterSeconds { get; set; }
    [BsonElement("enabled")] public bool Enabled { get; set; } = true;
}

[BsonIgnoreExtraElements]
public sealed class QueueSkillRelaxationRule
{
    [BsonElement("id")] public string Id { get; set; } = string.Empty;
    [BsonElement("waitSeconds")] public int WaitSeconds { get; set; }
    [BsonElement("action")] public string Action { get; set; } = "reduce_minimum_level";
    [BsonElement("scope")] public string Scope { get; set; } = "preferred";
    [BsonElement("reduction")] public int Reduction { get; set; }
    [BsonElement("destinationQueueId")] public string? DestinationQueueId { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueOverflowRule
{
    [BsonElement("id")] public string Id { get; set; } = string.Empty;
    [BsonElement("trigger")] public string Trigger { get; set; } = string.Empty;
    [BsonElement("action")] public string Action { get; set; } = string.Empty;
    [BsonElement("destinationId")] public string? DestinationId { get; set; }
    [BsonElement("delaySeconds")] public int DelaySeconds { get; set; }
    [BsonElement("priority")] public int Priority { get; set; }
    [BsonElement("enabled")] public bool Enabled { get; set; } = true;
}

[BsonIgnoreExtraElements]
public sealed class QueueSettings
{
    [BsonElement("capacity")] public QueueCapacitySettings Capacity { get; set; } = new();
    [BsonElement("ringing")] public QueueRingingSettings Ringing { get; set; } = new();
    [BsonElement("cx")] public QueueCxSettings Cx { get; set; } = new();
    [BsonElement("sla")] public QueueSlaSettings Sla { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class QueueCapacitySettings
{
    [BsonElement("maxQueueLength")] public int MaxQueueLength { get; set; } = 50;
    [BsonElement("maxWaitTimeSeconds")] public int MaxWaitTimeSeconds { get; set; } = 300;
    [BsonElement("maxConcurrentInteractions")] public int MaxConcurrentInteractions { get; set; } = 100;
    [BsonElement("maxConcurrentPerAgent")] public int MaxConcurrentPerAgent { get; set; } = 1;
}

[BsonIgnoreExtraElements]
public sealed class QueueRingingSettings
{
    [BsonElement("agentRingTimeoutSeconds")] public int AgentRingTimeoutSeconds { get; set; } = 20;
    [BsonElement("retryDelaySeconds")] public int RetryDelaySeconds { get; set; } = 5;
    [BsonElement("wrapUpTimeSeconds")] public int WrapUpTimeSeconds { get; set; } = 30;
    [BsonElement("wrapUpEnabled")] public bool WrapUpEnabled { get; set; } = true;
    [BsonElement("autoAnswer")] public bool AutoAnswer { get; set; }
    [BsonElement("allowReject")] public bool AllowReject { get; set; } = true;
    [BsonElement("reOfferRejected")] public bool ReOfferRejected { get; set; } = true;
}

[BsonIgnoreExtraElements]
public sealed class QueueCxSettings
{
    [BsonElement("musicOnHold")] public string MusicOnHold { get; set; } = string.Empty;
    [BsonElement("welcomeAnnouncement")] public string WelcomeAnnouncement { get; set; } = string.Empty;
    [BsonElement("periodicAnnouncement")] public string PeriodicAnnouncement { get; set; } = string.Empty;
    [BsonElement("announcementIntervalSeconds")] public int AnnouncementIntervalSeconds { get; set; } = 30;
    [BsonElement("positionAnnouncement")] public bool PositionAnnouncement { get; set; }
    [BsonElement("estimatedWaitAnnouncement")] public bool EstimatedWaitAnnouncement { get; set; }
    [BsonElement("callbackOffer")] public bool CallbackOffer { get; set; }
    [BsonElement("voicemailOffer")] public bool VoicemailOffer { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueSlaSettings
{
    [BsonElement("targetTimeSeconds")] public int TargetTimeSeconds { get; set; } = 20;
    [BsonElement("targetPercentage")] public int TargetPercentage { get; set; } = 80;
    [BsonElement("warningThreshold")] public int WarningThreshold { get; set; } = 70;
    [BsonElement("criticalThreshold")] public int CriticalThreshold { get; set; } = 60;
}

[BsonIgnoreExtraElements]
public sealed class QueueWorkingHours
{
    [BsonElement("mode")] public string Mode { get; set; } = "always";
    [BsonElement("existingScheduleId")] public string? ExistingScheduleId { get; set; }
    [BsonElement("timezone")] public string Timezone { get; set; } = "Asia/Riyadh";
    [BsonElement("schedule")] public Dictionary<string, QueueScheduleDay> Schedule { get; set; } = new();
    [BsonElement("outsideAction")] public string OutsideAction { get; set; } = "route_to_voicemail";
    [BsonElement("outsideDestinationId")] public string? OutsideDestinationId { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueScheduleDay
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("start")] public string Start { get; set; } = "09:00";
    [BsonElement("end")] public string End { get; set; } = "18:00";
}
