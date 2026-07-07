using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Billing;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ICurrentUser _currentUser;

    public BillingController(IBillingService billingService, ICurrentUser currentUser)
    {
        _billingService = billingService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BillingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBilling(CancellationToken ct)
    {
        var result = await _billingService.GetBillingAsync(_currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<BillingResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<BillingResponse>.Ok(result.Value));
    }

    [HttpPatch]
    [ProducesResponseType(typeof(ApiResponse<BillingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBilling([FromBody] UpdateBillingRequest request, CancellationToken ct)
    {
        var result = await _billingService.UpdateBillingAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<BillingResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<BillingResponse>.Ok(result.Value));
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse<BalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var result = await _billingService.GetBalanceAsync(_currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<BalanceResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<BalanceResponse>.Ok(result.Value));
    }

    [HttpPatch("balance")]
    [ProducesResponseType(typeof(ApiResponse<BalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBalanceSettings([FromBody] UpdateBalanceSettingsRequest request, CancellationToken ct)
    {
        var result = await _billingService.UpdateBalanceSettingsAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<BalanceResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<BalanceResponse>.Ok(result.Value));
    }
}
