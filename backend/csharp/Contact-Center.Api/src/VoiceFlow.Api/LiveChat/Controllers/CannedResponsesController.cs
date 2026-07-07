using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.LiveChat.Controllers;

[ApiController]
[Route("api/{projectId}/CannedResponses")]
public class CannedResponsesController : ControllerBase
{
    private readonly ICannedResponseRepository _repo;

    public CannedResponsesController(ICannedResponseRepository repo)
    {
        _repo = repo;
    }

    public sealed class CannedResponseInput
    {
        public string title { get; set; } = string.Empty;
        public List<string>? messages { get; set; }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CannedResponse>>>> GetAll(
        string projectId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return BadRequest(ApiResponse<IReadOnlyList<CannedResponse>>.Fail("projectId is required"));
        var items = await _repo.GetAllByProjectAsync(projectId, ct);
        return Ok(ApiResponse<IReadOnlyList<CannedResponse>>.Ok(items));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CannedResponse>>> GetById(
        string projectId, string id, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(projectId, id, ct);
        if (item is null) return NotFound(ApiResponse<CannedResponse>.Fail("notfound"));
        return Ok(ApiResponse<CannedResponse>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CannedResponse>>> Create(
        string projectId, [FromBody] CannedResponseInput body, CancellationToken ct)
    {
        var (err, messages) = Validate(body);
        if (err is not null) return BadRequest(ApiResponse<CannedResponse>.Fail(err));

        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var entity = new CannedResponse(projectId, createdBy, body.title.Trim())
        {
            messages = messages!,
        };
        await _repo.CreateAsync(entity, ct);
        return Ok(ApiResponse<CannedResponse>.Ok(entity));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CannedResponse>>> Update(
        string projectId, string id, [FromBody] CannedResponseInput body, CancellationToken ct)
    {
        var (err, messages) = Validate(body);
        if (err is not null) return BadRequest(ApiResponse<CannedResponse>.Fail(err));

        var update = new CannedResponse
        {
            title = body.title.Trim(),
            messages = messages!,
        };
        var updated = await _repo.UpdateAsync(projectId, id, update, ct);
        if (updated is null) return NotFound(ApiResponse<CannedResponse>.Fail("notfound"));
        return Ok(ApiResponse<CannedResponse>.Ok(updated));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        string projectId, string id, CancellationToken ct)
    {
        var ok = await _repo.DeleteAsync(projectId, id, ct);
        if (!ok) return NotFound(ApiResponse.Fail("notfound"));
        return Ok(ApiResponse.Ok());
    }

    private static (string? error, List<string>? messages) Validate(CannedResponseInput b)
    {
        if (string.IsNullOrWhiteSpace(b.title)) return ("title is required", null);
        var msgs = (b.messages ?? new List<string>())
            .Select(m => (m ?? string.Empty).Trim())
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();
        if (msgs.Count == 0) return ("at least one message is required", null);
        return (null, msgs);
    }
}
