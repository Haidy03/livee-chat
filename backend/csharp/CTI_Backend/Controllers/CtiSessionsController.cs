using CtiBackend.Models.Cti;
using CtiBackend.Models.Responses;
using CtiBackend.Services.Events;
using CtiBackend.Services.State;
using Microsoft.AspNetCore.Mvc;

namespace CtiBackend.Controllers;

[ApiController]
[Route("api/cti")]
public sealed class CtiSessionsController : ControllerBase
{
    private readonly ICallSessionStateManager _sessions;
    private readonly IAmiRawEventStore _events;

    public CtiSessionsController(ICallSessionStateManager sessions, IAmiRawEventStore events)
    {
        _sessions = sessions;
        _events = events;
    }

    [HttpGet("sessions/active")]
    public ActionResult<ApiResponse<IReadOnlyList<CallSessionState>>> Active() =>
        Ok(ApiResponse<IReadOnlyList<CallSessionState>>.Ok(_sessions.Active()));

    [HttpGet("sessions/recent-ended")]
    public ActionResult<ApiResponse<IReadOnlyList<CallSessionState>>> RecentEnded() =>
        Ok(ApiResponse<IReadOnlyList<CallSessionState>>.Ok(_sessions.RecentEnded()));

    [HttpGet("sessions/{sessionId}")]
    public ActionResult<ApiResponse<CallSessionState>> ById(string sessionId)
    {
        var s = _sessions.GetById(sessionId);
        return s is null ? NotFound(ApiResponse<CallSessionState>.Fail("not found"))
                         : Ok(ApiResponse<CallSessionState>.Ok(s));
    }

    [HttpGet("sessions/by-linkedid/{linkedId}")]
    public ActionResult<ApiResponse<CallSessionState>> ByLinkedId(string linkedId)
    {
        var s = _sessions.GetByLinkedId(linkedId);
        return s is null ? NotFound(ApiResponse<CallSessionState>.Fail("not found"))
                         : Ok(ApiResponse<CallSessionState>.Ok(s));
    }

    [HttpGet("sessions/by-uniqueid/{uniqueId}")]
    public ActionResult<ApiResponse<CallSessionState>> ByUniqueId(string uniqueId)
    {
        var s = _sessions.GetByUniqueId(uniqueId);
        return s is null ? NotFound(ApiResponse<CallSessionState>.Fail("not found"))
                         : Ok(ApiResponse<CallSessionState>.Ok(s));
    }

    [HttpGet("sessions/by-caller/{callerNumber}")]
    public ActionResult<ApiResponse<IReadOnlyList<CallSessionState>>> ByCaller(string callerNumber) =>
        Ok(ApiResponse<IReadOnlyList<CallSessionState>>.Ok(_sessions.GetByCaller(callerNumber)));

    [HttpGet("sessions/{sessionId}/journey")]
    public ActionResult<ApiResponse<IReadOnlyList<CallJourneyEvent>>> Journey(string sessionId)
    {
        var s = _sessions.GetById(sessionId);
        return s is null ? NotFound(ApiResponse<IReadOnlyList<CallJourneyEvent>>.Fail("not found"))
                         : Ok(ApiResponse<IReadOnlyList<CallJourneyEvent>>.Ok(s.Journey));
    }

    [HttpGet("events/recent")]
    public ActionResult<ApiResponse<object>> RecentEvents([FromQuery] int limit = 100) =>
        Ok(ApiResponse<object>.Ok(_events.Recent(limit)));
}
