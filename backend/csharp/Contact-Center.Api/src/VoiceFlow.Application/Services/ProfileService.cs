using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Profiles;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;

    public ProfileService(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result<IEnumerable<ProfileResponse>>> ListProfilesForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var profiles = await _profileRepository.GetByTenantAsync(tenantId, cancellationToken);
        var responses = profiles.Select(MapToResponse).ToList();
        return Result.Success<IEnumerable<ProfileResponse>>(responses);
    }

    public async Task<Result<ProfileResponse>> GetProfileAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAndTenantAsync(userId, tenantId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileResponse>(Error.NotFound("Profile", userId));

        return MapToResponse(profile);
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(string userId, string tenantId, PatchUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAndTenantAsync(userId, tenantId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileResponse>(Error.NotFound("Profile", userId));

        if (request.Email is not null) profile.Email = request.Email;
        if (request.FirstName is not null) profile.FirstName = request.FirstName;
        if (request.LastName is not null) profile.LastName = request.LastName;
        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName;
        if (request.ExtensionNumber.HasValue) profile.ExtensionNumber = request.ExtensionNumber;
        if (request.OutboundCid is not null) profile.OutboundCid = request.OutboundCid;
        if (request.Role is not null) profile.Role = NormalizeStoredRole(request.Role);
        if (request.Status is not null) profile.Status = request.Status;
        if (request.Disabled.HasValue) profile.Disabled = request.Disabled.Value;
        if (request.Language is not null) profile.Language = request.Language;
        if (request.Timezone is not null) profile.Timezone = request.Timezone;
        if (request.BrowserNotifications.HasValue) profile.BrowserNotifications = request.BrowserNotifications.Value;
        if (request.Groups is not null) profile.Groups = request.Groups.ToList();
        if (request.RecordInboundInternal.HasValue) profile.RecordInboundInternal = request.RecordInboundInternal.Value;
        if (request.RecordInboundExternal.HasValue) profile.RecordInboundExternal = request.RecordInboundExternal.Value;
        if (request.RecordOutboundInternal.HasValue) profile.RecordOutboundInternal = request.RecordOutboundInternal.Value;
        if (request.RecordOutboundExternal.HasValue) profile.RecordOutboundExternal = request.RecordOutboundExternal.Value;
        if (request.RecordOnDemand.HasValue) profile.RecordOnDemand = request.RecordOnDemand.Value;
        if (request.Skills is not null) profile.Skills = request.Skills.Select(x => new ProfileSkill()
        {
            Active = x.Active,
            Category = x.Category,
            Id = x.Id,
            Name = x.Name,
            Mandatory = x.Mandatory,
            Priority = x.Priority,
            Proficiency = x.Proficiency
        }).ToList();
        if (request.AvailableChannels is not null) profile.AvailableChannels = request.AvailableChannels.ToList();

        await _profileRepository.UpdateAsync(profile, cancellationToken);
        return MapToResponse(profile);
    }

    private static string NormalizeStoredRole(string role)
    {
        var r = role.Trim().ToLowerInvariant();
        return r switch
        {
            "owner" => "owner",
            "admin" => "admin",
            _ => "agent"
        };
    }

    private static ProfileResponse MapToResponse(Core.Entities.Profile profile) => new()
    {
        CreatedAt = profile.CreatedAt,
        Id = profile.Id,
        UserId = profile.UserId,
        TenantId = profile.TenantId,
        Email = profile.Email,
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        DisplayName = profile.DisplayName??"",
        Timezone = profile.Timezone,
        Language = profile.Language,
        BrowserNotifications = profile.BrowserNotifications,
        Role = profile.Role,
        Groups = profile.Groups,
        ExtensionNumber = profile.ExtensionNumber,
        OutboundCid = profile.OutboundCid,
        Status = profile.Status,
        Disabled = profile.Disabled,
        RecordInboundInternal = profile.RecordInboundInternal,
        RecordInboundExternal = profile.RecordInboundExternal,
        RecordOutboundInternal = profile.RecordOutboundInternal,
        RecordOutboundExternal = profile.RecordOutboundExternal,
        RecordOnDemand = profile.RecordOnDemand,
        Skills = profile.Skills.Select(x=> new ProfileSkillDto()
        {
            Active = x.Active,
            Category = x.Category,
            Id = x.Id,
            Name = x.Name,
            Mandatory = x.Mandatory,
            Priority = x.Priority,
            Proficiency = x.Proficiency
        }).ToList(),
        AvailableChannels = profile.AvailableChannels
    };
}
