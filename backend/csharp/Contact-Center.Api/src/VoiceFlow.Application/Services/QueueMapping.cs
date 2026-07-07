using VoiceFlow.Contracts.Queues;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Application.Services;

internal static class QueueMapping
{
    public static QueueResponse ToResponse(this Queue e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Code = e.Code,
        Description = e.Description,
        Channel = e.Channel,
        DepartmentId = e.DepartmentId,
        Status = e.Status,
        Tags = e.Tags ?? [],
        RoutingMode = e.RoutingMode,
        RoutingStrategy = e.RoutingStrategy,
        TieBreaker = e.TieBreaker,
        SkillMatchLogic = e.SkillMatchLogic,
        RequiredSkills = (e.RequiredSkills ?? []).Select(s => new QueueSkillRequirementDto
        {
            Id = s.Id,
            SkillId = s.SkillId,
            MinimumLevel = s.MinimumLevel,
            RequirementType = s.RequirementType,
            Weight = s.Weight,
        }).ToList(),
        Members = (e.Members ?? []).Select(m => new QueueMemberDto
        {
            Id = m.Id,
            AgentId = m.AgentId,
            Priority = m.Priority,
            Penalty = m.Penalty,
            Enabled = m.Enabled,
            MaxConcurrentInteractions = m.MaxConcurrentInteractions,
            WrapUpTimeOverride = m.WrapUpTimeOverride,
        }).ToList(),
        FallbackAgents = (e.FallbackAgents ?? []).Select(f => new QueueFallbackAgentDto
        {
            Id = f.Id,
            AgentId = f.AgentId,
            Priority = f.Priority,
            TriggerAfterSeconds = f.TriggerAfterSeconds,
            Enabled = f.Enabled,
        }).ToList(),
        SkillRelaxationEnabled = e.SkillRelaxationEnabled,
        SkillRelaxationRules = (e.SkillRelaxationRules ?? []).Select(r => new QueueSkillRelaxationRuleDto
        {
            Id = r.Id,
            WaitSeconds = r.WaitSeconds,
            Action = r.Action,
            Scope = r.Scope,
            Reduction = r.Reduction,
            DestinationQueueId = r.DestinationQueueId,
        }).ToList(),
        Settings = MapSettings(e.Settings),
        OverflowRules = (e.OverflowRules ?? []).Select(o => new QueueOverflowRuleDto
        {
            Id = o.Id,
            Trigger = o.Trigger,
            Action = o.Action,
            DestinationId = o.DestinationId,
            DelaySeconds = o.DelaySeconds,
            Priority = o.Priority,
            Enabled = o.Enabled,
        }).ToList(),
        WorkingHours = MapHours(e.WorkingHours),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    public static Queue ToEntity(this QueueWriteBase r)
    {
        var e = new Queue();
        r.ApplyTo(e);
        return e;
    }

    public static void ApplyTo(this QueueWriteBase r, Queue e)
    {
        e.Name = r.Name;
        e.Code = r.Code;
        e.Description = r.Description;
        e.Channel = r.Channel;
        e.DepartmentId = r.DepartmentId;
        e.Status = r.Status;
        e.Tags = r.Tags ?? [];
        e.RoutingMode = r.RoutingMode;
        e.RoutingStrategy = r.RoutingStrategy;
        e.TieBreaker = r.TieBreaker;
        e.SkillMatchLogic = r.SkillMatchLogic;
        e.RequiredSkills = (r.RequiredSkills ?? []).Select(s => new QueueSkillRequirement
        {
            Id = s.Id,
            SkillId = s.SkillId,
            MinimumLevel = s.MinimumLevel,
            RequirementType = s.RequirementType,
            Weight = s.Weight,
        }).ToList();
        e.Members = (r.Members ?? []).Select(m => new QueueMember
        {
            Id = m.Id,
            AgentId = m.AgentId,
            Priority = m.Priority,
            Penalty = m.Penalty,
            Enabled = m.Enabled,
            MaxConcurrentInteractions = m.MaxConcurrentInteractions,
            WrapUpTimeOverride = m.WrapUpTimeOverride,
        }).ToList();
        e.FallbackAgents = (r.FallbackAgents ?? []).Select(f => new QueueFallbackAgent
        {
            Id = f.Id,
            AgentId = f.AgentId,
            Priority = f.Priority,
            TriggerAfterSeconds = f.TriggerAfterSeconds,
            Enabled = f.Enabled,
        }).ToList();
        e.SkillRelaxationEnabled = r.SkillRelaxationEnabled;
        e.SkillRelaxationRules = (r.SkillRelaxationRules ?? []).Select(x => new QueueSkillRelaxationRule
        {
            Id = x.Id,
            WaitSeconds = x.WaitSeconds,
            Action = x.Action,
            Scope = x.Scope,
            Reduction = x.Reduction,
            DestinationQueueId = x.DestinationQueueId,
        }).ToList();
        e.Settings = ApplySettings(r.Settings);
        e.OverflowRules = (r.OverflowRules ?? []).Select(o => new QueueOverflowRule
        {
            Id = o.Id,
            Trigger = o.Trigger,
            Action = o.Action,
            DestinationId = o.DestinationId,
            DelaySeconds = o.DelaySeconds,
            Priority = o.Priority,
            Enabled = o.Enabled,
        }).ToList();
        e.WorkingHours = ApplyHours(r.WorkingHours);
    }

    public static Queue CloneForDuplicate(this Queue src)
    {
        var dto = src.ToResponse();
        var copy = new CreateQueueRequest
        {
            Name = $"{dto.Name} (Copy)",
            Code = $"{dto.Code}_COPY",
            Description = dto.Description,
            Channel = dto.Channel,
            DepartmentId = dto.DepartmentId,
            Status = "draft",
            Tags = dto.Tags.ToList(),
            RoutingMode = dto.RoutingMode,
            RoutingStrategy = dto.RoutingStrategy,
            TieBreaker = dto.TieBreaker,
            SkillMatchLogic = dto.SkillMatchLogic,
            RequiredSkills = dto.RequiredSkills,
            Members = dto.Members,
            FallbackAgents = dto.FallbackAgents,
            SkillRelaxationEnabled = dto.SkillRelaxationEnabled,
            SkillRelaxationRules = dto.SkillRelaxationRules,
            Settings = dto.Settings,
            OverflowRules = dto.OverflowRules,
            WorkingHours = dto.WorkingHours,
        };
        var entity = copy.ToEntity();
        entity.TenantId = src.TenantId;
        return entity;
    }

    private static QueueSettingsDto MapSettings(QueueSettings s) => new()
    {
        Capacity = new QueueCapacitySettingsDto
        {
            MaxQueueLength = s.Capacity.MaxQueueLength,
            MaxWaitTimeSeconds = s.Capacity.MaxWaitTimeSeconds,
            MaxConcurrentInteractions = s.Capacity.MaxConcurrentInteractions,
            MaxConcurrentPerAgent = s.Capacity.MaxConcurrentPerAgent,
        },
        Ringing = new QueueRingingSettingsDto
        {
            AgentRingTimeoutSeconds = s.Ringing.AgentRingTimeoutSeconds,
            RetryDelaySeconds = s.Ringing.RetryDelaySeconds,
            WrapUpTimeSeconds = s.Ringing.WrapUpTimeSeconds,
            WrapUpEnabled = s.Ringing.WrapUpEnabled,
            AutoAnswer = s.Ringing.AutoAnswer,
            AllowReject = s.Ringing.AllowReject,
            ReOfferRejected = s.Ringing.ReOfferRejected,
        },
        Cx = new QueueCxSettingsDto
        {
            MusicOnHold = s.Cx.MusicOnHold,
            WelcomeAnnouncement = s.Cx.WelcomeAnnouncement,
            PeriodicAnnouncement = s.Cx.PeriodicAnnouncement,
            AnnouncementIntervalSeconds = s.Cx.AnnouncementIntervalSeconds,
            PositionAnnouncement = s.Cx.PositionAnnouncement,
            EstimatedWaitAnnouncement = s.Cx.EstimatedWaitAnnouncement,
            CallbackOffer = s.Cx.CallbackOffer,
            VoicemailOffer = s.Cx.VoicemailOffer,
        },
        Sla = new QueueSlaSettingsDto
        {
            TargetTimeSeconds = s.Sla.TargetTimeSeconds,
            TargetPercentage = s.Sla.TargetPercentage,
            WarningThreshold = s.Sla.WarningThreshold,
            CriticalThreshold = s.Sla.CriticalThreshold,
        },
    };

    private static QueueSettings ApplySettings(QueueSettingsDto s) => new()
    {
        Capacity = new QueueCapacitySettings
        {
            MaxQueueLength = s.Capacity.MaxQueueLength,
            MaxWaitTimeSeconds = s.Capacity.MaxWaitTimeSeconds,
            MaxConcurrentInteractions = s.Capacity.MaxConcurrentInteractions,
            MaxConcurrentPerAgent = s.Capacity.MaxConcurrentPerAgent,
        },
        Ringing = new QueueRingingSettings
        {
            AgentRingTimeoutSeconds = s.Ringing.AgentRingTimeoutSeconds,
            RetryDelaySeconds = s.Ringing.RetryDelaySeconds,
            WrapUpTimeSeconds = s.Ringing.WrapUpTimeSeconds,
            WrapUpEnabled = s.Ringing.WrapUpEnabled,
            AutoAnswer = s.Ringing.AutoAnswer,
            AllowReject = s.Ringing.AllowReject,
            ReOfferRejected = s.Ringing.ReOfferRejected,
        },
        Cx = new QueueCxSettings
        {
            MusicOnHold = s.Cx.MusicOnHold,
            WelcomeAnnouncement = s.Cx.WelcomeAnnouncement,
            PeriodicAnnouncement = s.Cx.PeriodicAnnouncement,
            AnnouncementIntervalSeconds = s.Cx.AnnouncementIntervalSeconds,
            PositionAnnouncement = s.Cx.PositionAnnouncement,
            EstimatedWaitAnnouncement = s.Cx.EstimatedWaitAnnouncement,
            CallbackOffer = s.Cx.CallbackOffer,
            VoicemailOffer = s.Cx.VoicemailOffer,
        },
        Sla = new QueueSlaSettings
        {
            TargetTimeSeconds = s.Sla.TargetTimeSeconds,
            TargetPercentage = s.Sla.TargetPercentage,
            WarningThreshold = s.Sla.WarningThreshold,
            CriticalThreshold = s.Sla.CriticalThreshold,
        },
    };

    private static QueueWorkingHoursDto MapHours(QueueWorkingHours h) => new()
    {
        Mode = h.Mode,
        ExistingScheduleId = h.ExistingScheduleId,
        Timezone = h.Timezone,
        OutsideAction = h.OutsideAction,
        OutsideDestinationId = h.OutsideDestinationId,
        Schedule = (h.Schedule ?? new()).ToDictionary(
            kv => kv.Key,
            kv => new QueueScheduleDayDto { Enabled = kv.Value.Enabled, Start = kv.Value.Start, End = kv.Value.End }),
    };

    private static QueueWorkingHours ApplyHours(QueueWorkingHoursDto h) => new()
    {
        Mode = h.Mode,
        ExistingScheduleId = h.ExistingScheduleId,
        Timezone = h.Timezone,
        OutsideAction = h.OutsideAction,
        OutsideDestinationId = h.OutsideDestinationId,
        Schedule = (h.Schedule ?? new()).ToDictionary(
            kv => kv.Key,
            kv => new QueueScheduleDay { Enabled = kv.Value.Enabled, Start = kv.Value.Start, End = kv.Value.End }),
    };
}
