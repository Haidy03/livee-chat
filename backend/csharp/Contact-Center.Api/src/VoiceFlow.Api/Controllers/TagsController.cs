using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Tags;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tags")]
public sealed class TagsController : ControllerBase
{
    private readonly ITagService _service;
    private readonly ICurrentUser _currentUser;

    public TagsController(ITagService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetTagsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<TagResponse>>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken ct)
    {
        var result = await _service.CreateTagAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return Created(string.Empty, ApiResponse<TagResponse>.Ok(result.Value));
    }

    [HttpPatch("{tagId}")]
    public async Task<IActionResult> Update(string tagId, [FromBody] UpdateTagRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateTagAsync(tagId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<TagResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<TagResponse>.Ok(result.Value));
    }

    [HttpDelete("{tagId}")]
    public async Task<IActionResult> Delete(string tagId, CancellationToken ct)
    {
        var result = await _service.DeleteTagAsync(tagId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }
}
