using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.VoiceLibrary;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/voice-library")]
public sealed class VoiceLibraryController : ControllerBase
{
    private readonly IVoiceLibraryService _service;
    private readonly ICurrentUser _currentUser;

    public VoiceLibraryController(IVoiceLibraryService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetItemsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<VoiceLibraryItemResponse>>.Ok(result.Value));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _service.GetItemAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<VoiceLibraryItemResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<VoiceLibraryItemResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateVoiceLibraryItemRequest request, IFormFile? file, CancellationToken ct)
    {
        Stream? stream = file?.OpenReadStream();
        var result = await _service.CreateItemAsync(_currentUser.TenantId, _currentUser.UserId, request, stream, file?.FileName, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<VoiceLibraryItemResponse>.Ok(result.Value));
    }

    [HttpPost("tts")]
    public async Task<IActionResult> GenerateTts([FromBody] GenerateTtsRequest request, CancellationToken ct)
    {
        var result = await _service.GenerateTtsAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<VoiceLibraryItemResponse>.Ok(result.Value));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateVoiceLibraryItemRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateItemAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<VoiceLibraryItemResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<VoiceLibraryItemResponse>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteItemAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpGet("{id}/url")]
    public async Task<IActionResult> GetUrl(string id, CancellationToken ct)
    {
        var result = await _service.GetSignedUrlAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<SignedUrlResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<SignedUrlResponse>.Ok(result.Value));
    }
}
