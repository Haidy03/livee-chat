using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Accounts;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUser _currentUser;

    public AccountsController(IAccountService accountService, ICurrentUser currentUser)
    {
        _accountService = accountService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccount(CancellationToken ct)
    {
        var result = await _accountService.GetAccountAsync(_currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<AccountResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<AccountResponse>.Ok(result.Value));
    }

    [HttpPatch]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        var result = await _accountService.UpdateAccountAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<AccountResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<AccountResponse>.Ok(result.Value));
    }
}
