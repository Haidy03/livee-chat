using CtiBackend.Models.Responses;
using CtiBackend.Models.Responses.UsersMap;
using CtiBackend.Services.Ami;
using CtiBackend.Services.State.UsersMap;
using CtiBackend.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace CtiBackend.Controllers;

/// <summary>Real-time Users Map (caller-tracking) API. Mirrors Contact-Center UsersMapController snapshot.</summary>
[ApiController]
[Route("api/cti/users-map")]
[Produces("application/json")]
public sealed class UsersMapController : ControllerBase
{
    private readonly UsersMapSnapshotService _snapshot;
    private readonly ITenantContext _tenant;
    private readonly AmiConnectionContext _amiCtx;

    public UsersMapController(
        UsersMapSnapshotService snapshot,
        ITenantContext tenant,
        AmiConnectionContext amiCtx)
    {
        _snapshot = snapshot;
        _tenant = tenant;
        _amiCtx = amiCtx;
    }

    private string ResolveTenant(string? tenantId) =>
        !string.IsNullOrWhiteSpace(tenantId) ? tenantId!
        : !string.IsNullOrWhiteSpace(_tenant.TenantId) ? _tenant.TenantId!
        : _amiCtx.TenantId;

    /// <summary>Current snapshot of active callers, metrics, states and flow node counts.</summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApiResponse<UsersMapSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UsersMapSnapshotResponse>>> Snapshot(
        [FromQuery] string? tenantId,
        CancellationToken ct)
    {
        var data = await _snapshot.GetSnapshotAsync(ResolveTenant(tenantId), ct);
        return Ok(ApiResponse<UsersMapSnapshotResponse>.Ok(data));
    }
}
