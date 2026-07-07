using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.SipAccounts;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sip")]
public sealed class SipAccountsController : ControllerBase
{
    private readonly ISipAccountService _service;
    private readonly ICurrentUser _currentUser;

    public SipAccountsController(ISipAccountService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet("account")]
    public async Task<IActionResult> GetAccount(CancellationToken ct)
    {
        var result = await _service.GetSipAccountAsync(_currentUser.UserId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<SipAccountResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<SipAccountResponse>.Ok(result.Value));
    }

    [HttpPost("account")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateSipAccountRequest request, CancellationToken ct)
    {
        var result = await _service.CreateSipAccountAsync(_currentUser.UserId, _currentUser.TenantId, request, ct);
        return Created(string.Empty, ApiResponse<SipAccountResponse>.Ok(result.Value));
    }

    [HttpPatch("account")]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateSipAccountRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateSipAccountAsync(_currentUser.UserId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<SipAccountResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<SipAccountResponse>.Ok(result.Value));
    }

    [HttpGet("call-logs")]
    public async Task<IActionResult> GetCallLogs([FromQuery] PaginationRequest pagination, CancellationToken ct)
    {
        var result = await _service.GetSoftphoneCallLogsAsync(_currentUser.UserId, _currentUser.TenantId, pagination, ct);
        return Ok(ApiResponse<PagedResponse<SoftphoneCallLogResponse>>.Ok(result.Value));
    }

    [HttpPost("call-logs")]
    public async Task<IActionResult> CreateCallLog([FromBody] CreateSoftphoneCallLogRequest request, CancellationToken ct)
    {
        var result = await _service.CreateSoftphoneCallLogAsync(_currentUser.UserId, _currentUser.TenantId, request, ct);
        return Created(string.Empty, ApiResponse<SoftphoneCallLogResponse>.Ok(result.Value));
    }
}
