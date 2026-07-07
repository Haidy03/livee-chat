using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Groups;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/groups")]
public sealed class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ICurrentUser _currentUser;

    public GroupsController(IGroupService groupService, ICurrentUser currentUser)
    {
        _groupService = groupService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GroupResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _groupService.GetGroupsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<GroupResponse>>.Ok(result.Value));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _groupService.GetGroupAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<GroupResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<GroupResponse>.Ok(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var result = await _groupService.CreateGroupAsync(_currentUser.TenantId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse<GroupResponse>.Ok(result.Value));
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGroupRequest request, CancellationToken ct)
    {
        var result = await _groupService.UpdateGroupAsync(id, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<GroupResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<GroupResponse>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _groupService.DeleteGroupAsync(id, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }
}
