using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Flows;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/flows")]
public sealed class FlowsController : ControllerBase
{
    private readonly IFlowService _flowService;
    private readonly ICurrentUser _currentUser;

    public FlowsController(IFlowService flowService, ICurrentUser currentUser) { _flowService = flowService; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _flowService.GetFlowsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<FlowResponse>>.Ok(result.Value));
    }

    [HttpGet("{flowId}")]
    public async Task<IActionResult> Get(string flowId, CancellationToken ct)
    {
        var result = await _flowService.GetFlowAsync(flowId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<FlowResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<FlowResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFlowRequest request, CancellationToken ct)
    {
        var result = await _flowService.CreateFlowAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { flowId = result.Value.Id }, ApiResponse<FlowResponse>.Ok(result.Value));
    }

    [HttpPatch("{flowId}")]
    public async Task<IActionResult> Update(string flowId, [FromBody] UpdateFlowRequest request, CancellationToken ct)
    {
        var result = await _flowService.UpdateFlowAsync(flowId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<FlowResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<FlowResponse>.Ok(result.Value));
    }

    [HttpDelete("{flowId}")]
    public async Task<IActionResult> Delete(string flowId, CancellationToken ct)
    {
        var result = await _flowService.DeleteFlowAsync(flowId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpGet("{flowId}/validate")]
    public async Task<IActionResult> Validate(string flowId, CancellationToken ct)
    {
        var result = await _flowService.ValidateFlowAsync(flowId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<FlowValidationResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<FlowValidationResponse>.Ok(result.Value));
    }

    [HttpPost("{flowId}/publish")]
    public async Task<IActionResult> Publish(string flowId, [FromBody] PublishFlowRequest request, CancellationToken ct)
    {
        var result = await _flowService.PublishFlowAsync(flowId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<FlowResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<FlowResponse>.Ok(result.Value));
    }

    [HttpGet("{flowId}/export")]
    public async Task<IActionResult> Export(string flowId, CancellationToken ct)
    {
        var result = await _flowService.ExportFlowAsync(flowId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<FlowExportResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<FlowExportResponse>.Ok(result.Value));
    }
}
