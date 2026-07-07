using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Campaigns;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignService _service;
    private readonly ICurrentUser _currentUser;

    public CampaignsController(ICampaignService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<CampaignResponse>>.Ok(result.Value));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(_currentUser.TenantId, request, ct);
        return Created(string.Empty, ApiResponse<CampaignResponse>.Ok(result.Value));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCampaignRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignResponse>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpPost("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetCampaignStatusRequest request, CancellationToken ct)
    {
        var result = await _service.SetStatusAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignResponse>.Ok(result.Value));
    }

    // -------- Targets / Contacts (paginated) --------
    // The /contacts routes are aliases of /targets kept for the frontend client
    // which uses "contacts" terminology. Both URL forms hit the same service methods.

    [HttpGet("{id}/targets")]
    [HttpGet("{id}/contacts")]
    public async Task<IActionResult> ListTargets(
        string id,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var req = new ListCampaignTargetsRequest { Status = status, Search = search, Page = page, PageSize = pageSize };
        var result = await _service.ListTargetsAsync(id, _currentUser.TenantId, req, ct);
        if (result.IsFailure) return NotFound(ApiResponse<PagedResponse<CampaignContactDto>>.Fail(result.Error.Description));
        return Ok(ApiResponse<PagedResponse<CampaignContactDto>>.Ok(result.Value));
    }

    [HttpPost("{id}/targets")]
    [HttpPost("{id}/contacts")]
    public async Task<IActionResult> AddTargets(string id, [FromBody] AddCampaignContactsRequest request, CancellationToken ct)
    {
        var result = await _service.AddTargetsAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<int>.Fail(result.Error.Description));
        return Ok(ApiResponse<int>.Ok(result.Value));
    }

    [HttpDelete("{id}/targets/{targetId}")]
    [HttpDelete("{id}/contacts/{targetId}")]
    public async Task<IActionResult> RemoveTarget(string id, string targetId, CancellationToken ct)
    {
        var result = await _service.RemoveTargetAsync(id, _currentUser.TenantId, targetId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpPatch("{id}/targets/{targetId}/status")]
    [HttpPatch("{id}/contacts/{targetId}/status")]
    public async Task<IActionResult> UpdateTargetStatus(string id, string targetId, [FromBody] UpdateCampaignContactStatusRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateTargetStatusAsync(id, _currentUser.TenantId, targetId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignContactDto>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignContactDto>.Ok(result.Value));
    }

    // -------- Activity (paginated) --------

    [HttpGet("{id}/activity")]
    public async Task<IActionResult> ListActivity(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _service.ListActivityAsync(id, _currentUser.TenantId, page, pageSize, ct);
        if (result.IsFailure) return NotFound(ApiResponse<PagedResponse<CampaignActivityEntryDto>>.Fail(result.Error.Description));
        return Ok(ApiResponse<PagedResponse<CampaignActivityEntryDto>>.Ok(result.Value));
    }

    [HttpPost("{id}/activity")]
    public async Task<IActionResult> AddActivity(string id, [FromBody] AddCampaignActivityRequest request, CancellationToken ct)
    {
        var result = await _service.AddActivityAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignActivityEntryDto>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignActivityEntryDto>.Ok(result.Value));
    }

    // -------- Received calls (paginated) --------

    [HttpGet("{id}/received-calls")]
    public async Task<IActionResult> ListReceivedCalls(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _service.ListReceivedCallsAsync(id, _currentUser.TenantId, page, pageSize, ct);
        if (result.IsFailure) return NotFound(ApiResponse<PagedResponse<CampaignReceivedCallDto>>.Fail(result.Error.Description));
        return Ok(ApiResponse<PagedResponse<CampaignReceivedCallDto>>.Ok(result.Value));
    }

    [HttpPost("{id}/received-calls")]
    public async Task<IActionResult> AddReceivedCall(string id, [FromBody] AddCampaignReceivedCallRequest request, CancellationToken ct)
    {
        var result = await _service.AddReceivedCallAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<CampaignReceivedCallDto>.Fail(result.Error.Description));
        return Ok(ApiResponse<CampaignReceivedCallDto>.Ok(result.Value));
    }
}
