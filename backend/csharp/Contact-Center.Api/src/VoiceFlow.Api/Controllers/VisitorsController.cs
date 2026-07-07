using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Visitors;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/visitors")]
public sealed class VisitorsController : ControllerBase
{
    private readonly IVisitorsService _visitors;
    private readonly ICurrentUser _currentUser;

    public VisitorsController(IVisitorsService visitors, ICurrentUser currentUser)
    {
        _visitors = visitors;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] VisitorsQuery query, CancellationToken ct)
    {
        var result = await _visitors.SearchAsync(_currentUser.TenantId, query, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<PagedResponse<VisitorResponse>>.Fail(result.Error.Description));
        return Ok(ApiResponse<PagedResponse<VisitorResponse>>.Ok(result.Value));
    }
}
