using CtiBackend.Models.Cti;
using CtiBackend.Models.Requests;
using CtiBackend.Models.Responses;
using CtiBackend.Services.CallerInfo;
using CtiBackend.Services.State;
using Microsoft.AspNetCore.Mvc;

namespace CtiBackend.Controllers;

[ApiController]
[Route("api/cti")]
public sealed class CallerInfoController : ControllerBase
{
    private readonly ICallerInfoResolver _resolver;
    private readonly ICallSessionStateManager _sessions;

    public CallerInfoController(ICallerInfoResolver resolver, ICallSessionStateManager sessions)
    {
        _resolver = resolver;
        _sessions = sessions;
    }

    [HttpPost("caller-info/resolve")]
    public async Task<ActionResult<ApiResponse<object>>> Resolve(
        [FromBody] ResolveCallerInfoRequest body, CancellationToken ct)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.PhoneNumber))
            return BadRequest(ApiResponse<object>.Fail("phoneNumber required"));

        var info = await _resolver.ResolveAsync(body.TenantId, body.PhoneNumber, ct);
        return Ok(new { success = true, callerInfo = info });
    }

    [HttpPost("sessions/{sessionId}/refresh-caller-info")]
    public async Task<ActionResult<ApiResponse<CallSessionState>>> Refresh(string sessionId, CancellationToken ct)
    {
        var session = _sessions.GetById(sessionId);
        if (session is null) return NotFound(ApiResponse<CallSessionState>.Fail("not found"));
        if (string.IsNullOrWhiteSpace(session.CallerNumber))
            return BadRequest(ApiResponse<CallSessionState>.Fail("session has no callerNumber"));

        var info = await _resolver.ResolveAsync(session.TenantId, session.CallerNumber, ct);
        if (info != null) _sessions.UpdateCallerInfo(sessionId, info);
        return Ok(ApiResponse<CallSessionState>.Ok(_sessions.GetById(sessionId)!));
    }
}
