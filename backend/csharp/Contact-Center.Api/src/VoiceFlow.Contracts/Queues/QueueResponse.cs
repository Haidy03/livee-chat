namespace VoiceFlow.Contracts.Queues;

public sealed class QueueResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Channel { get; init; } = "voice";
    public string? DepartmentId { get; init; }
    public string Status { get; init; } = "draft";
    public List<string> Tags { get; init; } = [];
    public string RoutingMode { get; init; } = "static";
    public string RoutingStrategy { get; init; } = "ringall";
    public string TieBreaker { get; init; } = "longest_idle";
    public string SkillMatchLogic { get; init; } = "all";
    public List<QueueSkillRequirementDto> RequiredSkills { get; init; } = [];
    public List<QueueMemberDto> Members { get; init; } = [];
    public List<QueueFallbackAgentDto> FallbackAgents { get; init; } = [];
    public bool SkillRelaxationEnabled { get; init; }
    public List<QueueSkillRelaxationRuleDto> SkillRelaxationRules { get; init; } = [];
    public QueueSettingsDto Settings { get; init; } = new();
    public List<QueueOverflowRuleDto> OverflowRules { get; init; } = [];
    public QueueWorkingHoursDto WorkingHours { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class QueueWriteBase
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Channel { get; set; } = "voice";
    public string? DepartmentId { get; set; }
    public string Status { get; set; } = "draft";
    public List<string> Tags { get; set; } = [];
    public string RoutingMode { get; set; } = "static";
    public string RoutingStrategy { get; set; } = "ringall";
    public string TieBreaker { get; set; } = "longest_idle";
    public string SkillMatchLogic { get; set; } = "all";
    public List<QueueSkillRequirementDto> RequiredSkills { get; set; } = [];
    public List<QueueMemberDto> Members { get; set; } = [];
    public List<QueueFallbackAgentDto> FallbackAgents { get; set; } = [];
    public bool SkillRelaxationEnabled { get; set; }
    public List<QueueSkillRelaxationRuleDto> SkillRelaxationRules { get; set; } = [];
    public QueueSettingsDto Settings { get; set; } = new();
    public List<QueueOverflowRuleDto> OverflowRules { get; set; } = [];
    public QueueWorkingHoursDto WorkingHours { get; set; } = new();
}

public sealed class CreateQueueRequest : QueueWriteBase { }
public sealed class UpdateQueueRequest : QueueWriteBase { }

public sealed class QueueSkillRequirementDto
{
    public string Id { get; set; } = string.Empty;
    public string SkillId { get; set; } = string.Empty;
    public int MinimumLevel { get; set; }
    public string RequirementType { get; set; } = "mandatory";
    public int Weight { get; set; }
}

public sealed class QueueMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int Penalty { get; set; }
    public bool Enabled { get; set; } = true;
    public int? MaxConcurrentInteractions { get; set; }
    public int? WrapUpTimeOverride { get; set; }
}

public sealed class QueueFallbackAgentDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int TriggerAfterSeconds { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class QueueSkillRelaxationRuleDto
{
    public string Id { get; set; } = string.Empty;
    public int WaitSeconds { get; set; }
    public string Action { get; set; } = "reduce_minimum_level";
    public string Scope { get; set; } = "preferred";
    public int Reduction { get; set; }
    public string? DestinationQueueId { get; set; }
}

public sealed class QueueOverflowRuleDto
{
    public string Id { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? DestinationId { get; set; }
    public int DelaySeconds { get; set; }
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class QueueSettingsDto
{
    public QueueCapacitySettingsDto Capacity { get; set; } = new();
    public QueueRingingSettingsDto Ringing { get; set; } = new();
    public QueueCxSettingsDto Cx { get; set; } = new();
    public QueueSlaSettingsDto Sla { get; set; } = new();
}

public sealed class QueueCapacitySettingsDto
{
    public int MaxQueueLength { get; set; } = 50;
    public int MaxWaitTimeSeconds { get; set; } = 300;
    public int MaxConcurrentInteractions { get; set; } = 100;
    public int MaxConcurrentPerAgent { get; set; } = 1;
}

public sealed class QueueRingingSettingsDto
{
    public int AgentRingTimeoutSeconds { get; set; } = 20;
    public int RetryDelaySeconds { get; set; } = 5;
    public int WrapUpTimeSeconds { get; set; } = 30;
    public bool WrapUpEnabled { get; set; } = true;
    public bool AutoAnswer { get; set; }
    public bool AllowReject { get; set; } = true;
    public bool ReOfferRejected { get; set; } = true;
}

public sealed class QueueCxSettingsDto
{
    public string MusicOnHold { get; set; } = string.Empty;
    public string WelcomeAnnouncement { get; set; } = string.Empty;
    public string PeriodicAnnouncement { get; set; } = string.Empty;
    public int AnnouncementIntervalSeconds { get; set; } = 30;
    public bool PositionAnnouncement { get; set; }
    public bool EstimatedWaitAnnouncement { get; set; }
    public bool CallbackOffer { get; set; }
    public bool VoicemailOffer { get; set; }
}

public sealed class QueueSlaSettingsDto
{
    public int TargetTimeSeconds { get; set; } = 20;
    public int TargetPercentage { get; set; } = 80;
    public int WarningThreshold { get; set; } = 70;
    public int CriticalThreshold { get; set; } = 60;
}

public sealed class QueueWorkingHoursDto
{
    public string Mode { get; set; } = "always";
    public string? ExistingScheduleId { get; set; }
    public string Timezone { get; set; } = "Asia/Riyadh";
    public Dictionary<string, QueueScheduleDayDto> Schedule { get; set; } = new();
    public string OutsideAction { get; set; } = "route_to_voicemail";
    public string? OutsideDestinationId { get; set; }
}

public sealed class QueueScheduleDayDto
{
    public bool Enabled { get; set; }
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
}
