using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Rbac;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rbac")]
public sealed class RbacController : ControllerBase
{
    private readonly IRbacService _rbacService;
    private readonly ICurrentUser _currentUser;

    public RbacController(IRbacService rbacService, ICurrentUser currentUser)
    {
        _rbacService = rbacService;
        _currentUser = currentUser;
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await _rbacService.GetRolesAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<RoleResponse>>.Ok(result.Value));
    }

    [HttpGet("roles/{roleId}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRole(string roleId, CancellationToken ct)
    {
        var result = await _rbacService.GetRoleAsync(roleId, _currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<RoleResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<RoleResponse>.Ok(result.Value));
    }

    [HttpPost("roles")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await _rbacService.CreateRoleAsync(_currentUser.TenantId, request, ct);
        return CreatedAtAction(nameof(GetRole), new { roleId = result.Value.Id },
            ApiResponse<RoleResponse>.Ok(result.Value));
    }

    [HttpPatch("roles/{roleId}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await _rbacService.UpdateRoleAsync(roleId, _currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<RoleResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<RoleResponse>.Ok(result.Value));
    }

    [HttpDelete("roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken ct)
    {
        var result = await _rbacService.DeleteRoleAsync(roleId, _currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error.Description));

        return NoContent();
    }

    [HttpGet("assignments")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserRoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(CancellationToken ct)
    {
        var result = await _rbacService.GetAssignmentsAsync(_currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<UserRoleResponse>>.Ok(result.Value));
    }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(ApiResponse<UserRoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await _rbacService.AssignRoleAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<UserRoleResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<UserRoleResponse>.Ok(result.Value));
    }

    [HttpDelete("users/{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnassignRole(string userId, string roleId, CancellationToken ct)
    {
        await _rbacService.UnassignRoleAsync(userId, roleId, _currentUser.TenantId, ct);
        return NoContent();
    }

    [HttpGet("users/{userId}/roles")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserRoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(string userId, CancellationToken ct)
    {
        var result = await _rbacService.GetUserRolesAsync(userId, _currentUser.TenantId, ct);
        return Ok(ApiResponse<IEnumerable<UserRoleResponse>>.Ok(result.Value));
    }
}
