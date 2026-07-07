using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces.WrapUpCodes;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.WrapUpCodes.Requests;
using VoiceFlow.Contracts.WrapUpCodes.Responses;

namespace VoiceFlow.Api.Controllers.WrapUpCodes;

/// <summary>Wrap-up codes management (tenant-scoped) and per-queue mapping.</summary>
[ApiController]
[Authorize]
[Route("api/v1/wrap-up-codes")]
[Produces("application/json")]
public sealed class WrapUpCodesController : ControllerBase
{
    private readonly IWrapUpCodeService _service;
    private readonly ITenantContext _tenant;

    public WrapUpCodesController(IWrapUpCodeService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WrapUpCodeResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WrapUpCodeResponse>>>> List(
        [FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var data = await _service.ListAsync(_tenant.TenantId, activeOnly, ct);
        return Ok(ApiResponse<IReadOnlyList<WrapUpCodeResponse>>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WrapUpCodeResponse>>> Create(
        [FromBody] CreateWrapUpCodeRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.CreateAsync(_tenant.TenantId, request, ct);
            return Ok(ApiResponse<WrapUpCodeResponse>.Ok(data));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<WrapUpCodeResponse>.Fail(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<WrapUpCodeResponse>>> Update(
        string id, [FromBody] UpdateWrapUpCodeRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.UpdateAsync(_tenant.TenantId, id, request, ct);
            return Ok(ApiResponse<WrapUpCodeResponse>.Ok(data));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<WrapUpCodeResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<WrapUpCodeResponse>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(_tenant.TenantId, id, ct);
        if (!ok) return NotFound(ApiResponse<object>.Fail("notfound"));
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }

    // ---- Queue ↔ wrap-up code mapping ----

    [HttpGet("queues/{queueId}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> ListQueueCodeIds(
        string queueId, CancellationToken ct)
    {
        var ids = await _service.GetQueueCodeIdsAsync(_tenant.TenantId, queueId, ct);
        return Ok(ApiResponse<IReadOnlyList<string>>.Ok(ids));
    }

    [HttpPut("queues/{queueId}")]
    public async Task<ActionResult<ApiResponse<object>>> SetQueueCodes(
        string queueId, [FromBody] SetQueueWrapUpCodesRequest request, CancellationToken ct)
    {
        try
        {
            await _service.SetQueueCodesAsync(_tenant.TenantId, queueId, request.CodeIds ?? new(), ct);
            return Ok(ApiResponse<object>.Ok(new { ok = true }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("queues/{queueId}/effective")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WrapUpCodeResponse>>>> GetEffective(
        string queueId, CancellationToken ct)
    {
        var data = await _service.GetEffectiveForQueueAsync(_tenant.TenantId, queueId, ct);
        return Ok(ApiResponse<IReadOnlyList<WrapUpCodeResponse>>.Ok(data));
    }
}
