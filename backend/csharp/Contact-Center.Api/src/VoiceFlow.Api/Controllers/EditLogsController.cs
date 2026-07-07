using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.EditLogs;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit")]
public sealed class EditLogsController : ControllerBase
{
    private readonly IEditLogService _service;
    private readonly ICurrentUser _currentUser;

    public EditLogsController(IEditLogService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] EditLogSearchRequest request, CancellationToken ct)
    {
        var result = await _service.SearchAsync(_currentUser.TenantId, request, ct);
        return Ok(ApiResponse<PagedResponse<EditLogResponse>>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEditLogRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<EditLogResponse>.Fail(result.Error.Description));
        return Created(string.Empty, ApiResponse<EditLogResponse>.Ok(result.Value));
    }
}
