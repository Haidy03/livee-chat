using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Voicemail;

namespace VoiceFlow.Api.Controllers;

/// <summary>
/// Agent voicemail inbox. Ingest happens out-of-band: the Asterisk dialplan emits
/// UserEvent(VoicemailRecorded), the ContactCenterService worker catches it over AMI and
/// stores + processes the recording. This controller is the authenticated read/action side.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/voicemails")]
public sealed class VoicemailController : ControllerBase
{
    private readonly IVoicemailService _service;
    private readonly ICurrentUser _currentUser;

    public VoicemailController(IVoicemailService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var items = await _service.ListForAgentAsync(_currentUser.TenantId, _currentUser.UserId, status, ct);
        return Ok(ApiResponse<IReadOnlyList<VoicemailResponse>>.Ok(items));
    }

    [HttpGet("unread-counts")]
    public async Task<IActionResult> UnreadCounts(CancellationToken ct)
    {
        var counts = await _service.UnreadCountsForAgentAsync(_currentUser.TenantId, _currentUser.UserId, ct);
        return Ok(ApiResponse<IReadOnlyDictionary<string, int>>.Ok(counts));
    }

    [HttpPost("{id}/claim")]
    public async Task<IActionResult> Claim(string id, CancellationToken ct)
    {
        var ok = await _service.ClaimAsync(_currentUser.TenantId, _currentUser.UserId, id, ct);
        if (!ok) return Conflict(ApiResponse.Fail("Voicemail already claimed."));
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(string id, CancellationToken ct)
    {
        var ok = await _service.ResolveAsync(_currentUser.TenantId, _currentUser.UserId, id, ct);
        if (!ok) return NotFound(ApiResponse.Fail("Voicemail not found."));
        return Ok(ApiResponse.Ok());
    }
}
