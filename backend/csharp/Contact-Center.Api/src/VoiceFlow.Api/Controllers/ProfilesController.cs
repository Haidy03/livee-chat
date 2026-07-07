using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Profiles;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUser _currentUser;

    public ProfilesController(IProfileService profileService, ICurrentUser currentUser)
    {
        _profileService = profileService;
        _currentUser = currentUser;
    }

    private static bool IsElevated(ICurrentUser user) =>
        user.Roles.Any(r => string.Equals(r, "owner", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase));

    private static bool IsOwner(ICurrentUser user) =>
        user.Roles.Any(r => string.Equals(r, "owner", StringComparison.OrdinalIgnoreCase));

    private static PatchUserProfileRequest WithoutTenantAdminFields(PatchUserProfileRequest r) =>
        new()
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            DisplayName = r.DisplayName,
            Timezone = r.Timezone,
            Language = r.Language,
            BrowserNotifications = r.BrowserNotifications,
            Status = r.Status,
            ExtensionNumber = r.ExtensionNumber,
            OutboundCid = r.OutboundCid,
            Disabled = r.Disabled,
            RecordInboundInternal = r.RecordInboundInternal,
            RecordInboundExternal = r.RecordInboundExternal,
            RecordOutboundInternal = r.RecordOutboundInternal,
            RecordOutboundExternal = r.RecordOutboundExternal,
            RecordOnDemand = r.RecordOnDemand,
            Skills = r.Skills,
            Email = r.Email,
            Groups = r.Groups,
            Role = r.Role,
            AvailableChannels = r.AvailableChannels
        };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProfileResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProfiles(CancellationToken ct)
    {
        var result = await _profileService.ListProfilesForTenantAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<ProfileResponse>>.Ok(result.Value));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await _profileService.GetProfileAsync(_currentUser.UserId, _currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<ProfileResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<ProfileResponse>.Ok(result.Value));
    }

    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] PatchUserProfileRequest request, CancellationToken ct)
    {
        var limited = WithoutTenantAdminFields(request);
        var result = await _profileService.UpdateProfileAsync(_currentUser.UserId, _currentUser.TenantId, limited, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<ProfileResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<ProfileResponse>.Ok(result.Value));
    }

    /// <summary>Tenant admins update another profile in the same tenant (role changes: owners only).</summary>
    [HttpPatch("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTenantProfile(string userId, [FromBody] PatchUserProfileRequest request, CancellationToken ct)
    {
        var self = string.Equals(userId, _currentUser.UserId, StringComparison.Ordinal);
        if (!self && !IsElevated(_currentUser))
            return Forbid();

        if (!self && request.Role is not null && !IsOwner(_currentUser))
            return Forbid();

        var body = self ? WithoutTenantAdminFields(request) : request;
        var result = await _profileService.UpdateProfileAsync(userId, _currentUser.TenantId, body, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<ProfileResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<ProfileResponse>.Ok(result.Value));
    }
}
