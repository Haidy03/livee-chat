using CtiBackend.Options;
using CtiBackend.Services.Ami;
using CtiBackend.Services.QueueMonitoring;
using CtiBackend.Services.QueueMonitoring.Models;
using CtiBackend.Tenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CtiBackend.Controllers;

[ApiController]
[Route("api/cti/monitoring")]
public sealed class QueueMonitoringController : ControllerBase
{
    private readonly IQueueStateQueryService _query;
    private readonly IQueueSnapshotService _snapshot;
    private readonly AmiConnectionContext _amiCtx;
    private readonly QueueMonitoringOptions _opts;
    private readonly ITenantContext _tenant;

    public QueueMonitoringController(
        IQueueStateQueryService query,
        IQueueSnapshotService snapshot,
        AmiConnectionContext amiCtx,
        IOptions<QueueMonitoringOptions> opts,
        ITenantContext tenant)
    {
        _query = query;
        _snapshot = snapshot;
        _amiCtx = amiCtx;
        _opts = opts.Value;
        _tenant = tenant;
    }

    private string ResolveTenant(string? tenantId) =>
        !string.IsNullOrWhiteSpace(tenantId) ? tenantId!
        : !string.IsNullOrWhiteSpace(_tenant.TenantId) ? _tenant.TenantId!
        : _amiCtx.TenantId;
    private string Server(string? serverId) => string.IsNullOrWhiteSpace(serverId) ? _amiCtx.ServerId : serverId!;

    [HttpGet("queues")]
    public async Task<ActionResult<IReadOnlyCollection<QueueLiveState>>> Queues([FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetQueuesAsync(ResolveTenant(tenantId), Server(serverId), ct));

    [HttpGet("queues/{queueName}")]
    public async Task<ActionResult<QueueLiveState>> Queue(string queueName, [FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var q = await _query.GetQueueAsync(ResolveTenant(tenantId), Server(serverId), queueName, ct);
        return q is null ? NotFound() : Ok(q);
    }

    [HttpGet("queues/{queueName}/agents")]
    public async Task<ActionResult<IReadOnlyCollection<QueueAgentLiveState>>> QueueAgents(
        string queueName,
        [FromQuery] string? tenantId,
        [FromQuery] string? serverId,
        [FromQuery] string? status,
        [FromQuery] bool? paused,
        [FromQuery] bool? inCall,
        CancellationToken ct)
    {
        var agents = await _query.GetQueueAgentsAsync(ResolveTenant(tenantId), Server(serverId), queueName, ct);
        IEnumerable<QueueAgentLiveState> filtered = agents;
        if (!string.IsNullOrEmpty(status))
            filtered = filtered.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
        if (paused.HasValue)
            filtered = filtered.Where(a => a.Paused == paused.Value);
        if (inCall.HasValue)
            filtered = filtered.Where(a => a.InCall == inCall.Value);
        return Ok(filtered.ToArray());
    }

    [HttpGet("queues/{queueName}/waiting-callers")]
    public async Task<ActionResult<IReadOnlyCollection<QueueWaitingCallerState>>> Waiting(
        string queueName, [FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetWaitingCallersAsync(ResolveTenant(tenantId), Server(serverId), queueName, ct));

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyCollection<QueueAgentLiveState>>> Agents([FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetAgentsAsync(ResolveTenant(tenantId), Server(serverId), ct));

    [HttpGet("agents/{agentId}")]
    public async Task<ActionResult<QueueAgentLiveState>> Agent(string agentId, [FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var a = await _query.GetAgentAsync(ResolveTenant(tenantId), Server(serverId), agentId, ct);
        return a is null ? NotFound() : Ok(a);
    }

    [HttpGet("status")]
    public async Task<ActionResult<AmiServerStatus?>> Status([FromQuery] string? tenantId, [FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetServerStatusAsync(ResolveTenant(tenantId), Server(serverId), ct));

    [HttpPost("refresh")]
    public async Task<ActionResult> Refresh(CancellationToken ct)
    {
        if (!_opts.EnableAdministrativeRefresh) return StatusCode(StatusCodes.Status403Forbidden, new { error = "refresh disabled" });
        var opId = await _snapshot.RequestFullSnapshotAsync(_amiCtx, ct);
        return Accepted(new { operationId = opId, status = string.IsNullOrEmpty(opId) ? "skipped" : "started" });
    }
}
