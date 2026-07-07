using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.UserMaps.Requests;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.UserMaps.Responses;


namespace VoiceFlow.Reports.Api.Controllers.UserMappings;

/// <summary>Real-time Users Map (caller-tracking) API.</summary>
[ApiController]
[Authorize]
[Route("api/v1/live/users-map")]
[Produces("application/json")]
public sealed class UsersMapController : ControllerBase
{
    private readonly IUsersMapService _service;
    private readonly ITenantContext _tenant;

    public UsersMapController(IUsersMapService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Current snapshot of active callers, metrics, states and flow node counts.</summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApiResponse<UsersMapSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UsersMapSnapshotResponse>>> Snapshot(CancellationToken ct)
    {
        var data = await _service.GetSnapshotAsync(_tenant.TenantId, ct);
        return Ok(ApiResponse<UsersMapSnapshotResponse>.Ok(data));
    }

    [HttpPost("calls/{callId}/listen")]
    public Task<ActionResult<ApiResponse<object>>> Listen(string callId, CancellationToken ct) => Act(callId, "listen", null, ct);

    [HttpPost("calls/{callId}/whisper")]
    public Task<ActionResult<ApiResponse<object>>> Whisper(string callId, CancellationToken ct) => Act(callId, "whisper", null, ct);

    [HttpPost("calls/{callId}/barge")]
    public Task<ActionResult<ApiResponse<object>>> Barge(string callId, CancellationToken ct) => Act(callId, "barge", null, ct);

    [HttpPost("calls/{callId}/hangup")]
    public Task<ActionResult<ApiResponse<object>>> Hangup(string callId, CancellationToken ct) => Act(callId, "hangup", null, ct);

    [HttpPost("calls/{callId}/transfer")]
    public Task<ActionResult<ApiResponse<object>>> Transfer(string callId, [FromBody] TransferRequest body, CancellationToken ct) => Act(callId, "transfer", body, ct);

    private async Task<ActionResult<ApiResponse<object>>> Act(string callId, string kind, TransferRequest? body, CancellationToken ct)
    {
        await _service.ActionAsync(_tenant.TenantId, callId, kind, body, ct);
        return Accepted(ApiResponse<object>.Ok(new { ok = true, kind }));
    }
}
