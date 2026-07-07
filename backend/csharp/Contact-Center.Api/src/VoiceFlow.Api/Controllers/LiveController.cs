using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Live;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/live")]
public sealed class LiveController : ControllerBase
{
    private readonly ILiveSnapshotService _liveSnapshotService;
    private readonly ICurrentUser _currentUser;

    public LiveController(ILiveSnapshotService liveSnapshotService, ICurrentUser currentUser)
    {
        _liveSnapshotService = liveSnapshotService;
        _currentUser = currentUser;
    }

    /// <summary>Active calls, queue metrics, and agent presence for the tenant (poll every few seconds).</summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApiResponse<LiveSnapshot>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshot(CancellationToken ct)
    {
        var result = await _liveSnapshotService.GetSnapshotAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<LiveSnapshot>.Ok(result.Value));
    }
}
