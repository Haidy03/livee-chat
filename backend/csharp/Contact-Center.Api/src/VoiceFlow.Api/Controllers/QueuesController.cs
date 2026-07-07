using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Queues;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/queues")]
public sealed class QueuesController : ControllerBase
{
    private readonly IQueueService _service;
    private readonly ICurrentUser _currentUser;

    public QueuesController(IQueueService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QueueResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetQueuesAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<QueueResponse>>.Ok(result.Value));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _service.GetQueueAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<QueueResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<QueueResponse>.Ok(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateQueueRequest request, CancellationToken ct)
    {
        var result = await _service.CreateQueueAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<QueueResponse>.Fail(result.Error.Description));
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<QueueResponse>.Ok(result.Value));
    }

    [HttpPatch("{id}")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQueueRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateQueueAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure)
        {
            if (result.Error.Code.EndsWith(".NotFound"))
                return NotFound(ApiResponse<QueueResponse>.Fail(result.Error.Description));
            return BadRequest(ApiResponse<QueueResponse>.Fail(result.Error.Description));
        }
        return Ok(ApiResponse<QueueResponse>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteQueueAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(string id, CancellationToken ct)
    {
        var result = await _service.DuplicateQueueAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<QueueResponse>.Fail(result.Error.Description));
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<QueueResponse>.Ok(result.Value));
    }

    [HttpPost("{id}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken ct)
    {
        var result = await _service.ToggleStatusAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<QueueResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<QueueResponse>.Ok(result.Value));
    }
}
