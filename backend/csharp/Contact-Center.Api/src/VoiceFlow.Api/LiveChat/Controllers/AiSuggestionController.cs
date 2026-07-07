using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.LiveChat.Application.AiSuggestions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.LiveChat.Controllers;

[ApiController]
[Authorize]
[Route("api/{projectId}/AiSuggestions")]
public sealed class AiSuggestionController : ControllerBase
{
    private readonly IAiSuggestionService _service;

    public AiSuggestionController(IAiSuggestionService service) => _service = service;

    private string? AgentId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<AiSuggestionResponse>>> Generate(
        string projectId, [FromBody] AiSuggestionRequest body, CancellationToken ct)
    {
        var agentId = AgentId;
        if (string.IsNullOrEmpty(agentId)) return Unauthorized(ApiResponse<AiSuggestionResponse>.Fail("no_agent"));
        try
        {
            var res = await _service.GenerateAsync(projectId, agentId, body, ct);
            return Ok(ApiResponse<AiSuggestionResponse>.Ok(res));
        }
        catch (AiSuggestDisabledException ex)
        {
            return StatusCode(503, ApiResponse<AiSuggestionResponse>.Fail(ex.Message));
        }
        catch (AiSuggestRateLimitException ex)
        {
            return StatusCode(429, ApiResponse<AiSuggestionResponse>.Fail(ex.Message));
        }
        catch (AiSuggestForbiddenException ex)
        {
            return StatusCode(403, ApiResponse<AiSuggestionResponse>.Fail(ex.Message));
        }
    }

    public sealed class MarkUsedBody
    {
        public string UsedText { get; set; } = string.Empty;
        public string? SentMessageId { get; set; }
    }

    [HttpPost("{suggestionId}/used")]
    public async Task<ActionResult<ApiResponse>> MarkUsed(
        string projectId, string suggestionId, [FromBody] MarkUsedBody body, CancellationToken ct)
    {
        var agentId = AgentId;
        if (string.IsNullOrEmpty(agentId)) return Unauthorized(ApiResponse.Fail("no_agent"));
        try
        {
            await _service.MarkUsedAsync(projectId, agentId, suggestionId, body.UsedText ?? string.Empty, body.SentMessageId, ct);
            return Ok(ApiResponse.Ok());
        }
        catch (AiSuggestNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<ApiResponse>> Feedback(
        string projectId, [FromBody] AiSuggestionFeedbackRequest body, CancellationToken ct)
    {
        var agentId = AgentId;
        if (string.IsNullOrEmpty(agentId)) return Unauthorized(ApiResponse.Fail("no_agent"));
        try
        {
            await _service.AddFeedbackAsync(projectId, agentId, body, ct);
            return Ok(ApiResponse.Ok());
        }
        catch (AiSuggestNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("byRoom/{roomId}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiSuggestion>>>> ByRoom(
        string projectId, string roomId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var items = await _service.ListByRoomAsync(projectId, roomId, limit, ct);
        return Ok(ApiResponse<IReadOnlyList<AiSuggestion>>.Ok(items));
    }
}
