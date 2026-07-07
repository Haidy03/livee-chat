using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces.QueueMonitoring;
using VoiceFlow.Application.Options;
using VoiceFlow.Contracts.Queues.Monitoring;

namespace VoiceFlow.Api.Controllers.Monitoring;

/// <summary>
/// Read-only queue / agent live state served from Redis (written by CTI_Backend).
/// </summary>
[ApiController]
[Authorize]
[Route("api/cti/monitoring")]
[Produces("application/json")]
public sealed class QueueMonitoringController : ControllerBase
{
    private readonly IQueueStateQueryService _query;
    private readonly ITenantContext _tenant;
    private readonly QueueMonitoringOptions _opts;

    public QueueMonitoringController(
        IQueueStateQueryService query,
        ITenantContext tenant,
        IOptions<QueueMonitoringOptions> opts)
    {
        _query = query;
        _tenant = tenant;
        _opts = opts.Value;
    }

    private string Tenant => _tenant.TenantId;
    private string Server(string? serverId) => string.IsNullOrWhiteSpace(serverId) ? _opts.DefaultServerId : serverId!;

    [HttpGet("queues")]
    public async Task<ActionResult<IReadOnlyCollection<QueueLiveStateDto>>> Queues([FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetQueuesAsync(Tenant, Server(serverId), ct));

    [HttpGet("queues/{queueName}")]
    public async Task<ActionResult<QueueLiveStateDto>> Queue(string queueName, [FromQuery] string? serverId, CancellationToken ct)
    {
        var q = await _query.GetQueueAsync(Tenant, Server(serverId), queueName, ct);
        return q is null ? NotFound() : Ok(q);
    }

    [HttpGet("queues/{queueName}/agents")]
    public async Task<ActionResult<IReadOnlyCollection<QueueAgentLiveStateDto>>> QueueAgents(
        string queueName,
        [FromQuery] string? serverId,
        [FromQuery] string? status,
        [FromQuery] bool? paused,
        [FromQuery] bool? inCall,
        CancellationToken ct)
    {
        var agents = await _query.GetQueueAgentsAsync(Tenant, Server(serverId), queueName, ct);
        IEnumerable<QueueAgentLiveStateDto> filtered = agents;
        if (!string.IsNullOrEmpty(status))
            filtered = filtered.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
        if (paused.HasValue)
            filtered = filtered.Where(a => a.Paused == paused.Value);
        if (inCall.HasValue)
            filtered = filtered.Where(a => a.InCall == inCall.Value);
        return Ok(filtered.ToArray());
    }

    [HttpGet("queues/{queueName}/waiting-callers")]
    public async Task<ActionResult<IReadOnlyCollection<QueueWaitingCallerStateDto>>> Waiting(
        string queueName, [FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetWaitingCallersAsync(Tenant, Server(serverId), queueName, ct));

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyCollection<QueueAgentLiveStateDto>>> Agents([FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetAgentsAsync(Tenant, Server(serverId), ct));

    [HttpGet("agents/{agentId}")]
    public async Task<ActionResult<QueueAgentLiveStateDto>> Agent(string agentId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var a = await _query.GetAgentAsync(Tenant, Server(serverId), agentId, ct);
        return a is null ? NotFound() : Ok(a);
    }

    [HttpGet("status")]
    public async Task<ActionResult<AmiServerStatusDto?>> Status([FromQuery] string? serverId, CancellationToken ct)
        => Ok(await _query.GetServerStatusAsync(Tenant, Server(serverId), ct));
}
