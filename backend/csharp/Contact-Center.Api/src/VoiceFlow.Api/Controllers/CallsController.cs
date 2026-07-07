using FirebaseAdmin.Auth.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.Calls;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/calls")]
public sealed class CallsController : ControllerBase
{
    private readonly ICallService _callService;
    private readonly ICurrentUser _currentUser;

    public CallsController(ICallService callService, ICurrentUser currentUser)
    {
        _callService = callService;
        _currentUser = currentUser;
    }

    [HttpPost("sip-upsert")]
    public async Task<IActionResult> UpsertSoftphoneCall([FromBody] SoftphoneCallUpsertRequest request, CancellationToken ct)
    {
        var result = await _callService.UpsertSoftphoneCallAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return Ok(ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] CallSearchRequest request, CancellationToken ct)
    {
        var result = await _callService.SearchCallsAsync(_currentUser.TenantId, request, ct);
        return Ok(ApiResponse<PagedResponse<CallResponse>>.Ok(result.Value));
    }

    [HttpPost("search")]
    public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedCallSearchRequest request, CancellationToken ct)
    {
        var result = await _callService.AdvancedSearchCallsAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<CallSearchResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<CallSearchResponse>.Ok(result.Value));
    }

    [HttpGet("filters/options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken ct)
    {
        var result = await _callService.GetCallFilterOptionsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<CallFilterOptions>.Ok(result.Value));
    }

    [HttpGet("{callId}")]
    public async Task<IActionResult> Get(string callId, CancellationToken ct)
    {
        var result = await _callService.GetCallAsync(callId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CallResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCallRequest request, CancellationToken ct)
    {
        var result = await _callService.CreateCallAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { callId = result.Value.Id }, ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpPatch("{callId}")]
    public async Task<IActionResult> Update(string callId, [FromBody] UpdateCallRequest request, CancellationToken ct)
    {
        var result = await _callService.UpdateCallAsync(callId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CallResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpDelete("{callId}")]
    public async Task<IActionResult> Delete(string callId, CancellationToken ct)
    {
        var result = await _callService.DeleteCallAsync(callId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpGet("{callId}/recording")]
    public async Task<IActionResult> GetRecording(string callId, CancellationToken ct)
    {
        var result = await _callService.GetRecordingUrlAsync(callId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<SignedUrlResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<SignedUrlResponse>.Ok(result.Value));
    }

    [HttpPost("{callId}/summary")]
    public async Task<IActionResult> GenerateSummary(string callId, [FromBody] GenerateSummaryRequest request, CancellationToken ct)
    {
        var result = await _callService.GenerateSummaryAsync(callId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<CallResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpPost("{callId}/translate")]
    public async Task<IActionResult> TranslateSummary(string callId, [FromBody] TranslateSummaryRequest request, CancellationToken ct)
    {
        var result = await _callService.TranslateSummaryAsync(callId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<CallResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CallResponse>.Ok(result.Value));
    }

    [HttpPost("wrap-up")]
    [ProducesResponseType(typeof(ApiResponse<WrapUpCallResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WrapUpCallResponse>>> WrapUp(
       [FromBody] WrapUpCallRequest request, CancellationToken ct)
    {
       
         var data = await _callService.SaveWrapUpAsync(_currentUser.TenantId, request, ct);
         return Ok(ApiResponse<WrapUpCallResponse>.Ok(data));
        
    }


}
