using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.AutoTags;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/auto-tags")]
public sealed class AutoTagsController : ControllerBase
{
    private readonly IAutoTagService _service;
    private readonly ICurrentUser _currentUser;

    public AutoTagsController(IAutoTagService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAutoTagsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<AutoTagResponse>>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAutoTagRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAutoTagAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return Created(string.Empty, ApiResponse<AutoTagResponse>.Ok(result.Value));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] JsonElement patch, CancellationToken ct)
    {
        var result = await _service.UpdateAutoTagAsync(id, _currentUser.TenantId, patch, ct);
        if (result.IsFailure) return NotFound(ApiResponse<AutoTagResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<AutoTagResponse>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAutoTagAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }
}
