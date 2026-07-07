using Microsoft.Extensions.Logging;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Infrastructure.ExternalServices;

/// <summary>
/// No-op call-control client. Logs the requested supervisor action and
/// returns success. A real FreeSWITCH ESL / Asterisk AMI implementation can
/// replace this binding without touching the rest of the stack.
/// </summary>
public sealed class NoopCallControlClient : ICallControlClient
{
    private readonly ILogger<NoopCallControlClient> _log;
    public NoopCallControlClient(ILogger<NoopCallControlClient> log) => _log = log;

    public Task<bool> ListenAsync(string tenantId, string callId, CancellationToken ct) => Log("listen", tenantId, callId);
    public Task<bool> WhisperAsync(string tenantId, string callId, CancellationToken ct) => Log("whisper", tenantId, callId);
    public Task<bool> BargeAsync(string tenantId, string callId, CancellationToken ct) => Log("barge", tenantId, callId);
    public Task<bool> HangupAsync(string tenantId, string callId, CancellationToken ct) => Log("hangup", tenantId, callId);

    public Task<bool> TransferAsync(string tenantId, string callId, string targetType, string? targetId, string? targetNumber, CancellationToken ct)
    {
        _log.LogInformation("CallControl[noop] transfer tenant={Tenant} call={Call} type={Type} id={Id} number={Number}",
            tenantId, callId, targetType, targetId, targetNumber);
        return Task.FromResult(true);
    }

    private Task<bool> Log(string action, string tenantId, string callId)
    {
        _log.LogInformation("CallControl[noop] {Action} tenant={Tenant} call={Call}", action, tenantId, callId);
        return Task.FromResult(true);
    }
}
