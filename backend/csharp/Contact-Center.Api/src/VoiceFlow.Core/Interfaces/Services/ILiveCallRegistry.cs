using VoiceFlow.Core.Models;
namespace VoiceFlow.Core.Interfaces.Services;

/// <summary>
/// Storage for live caller state. Fed by dialplan ingest events,
/// read by UsersMapService to build the snapshot response.
/// </summary>
public interface ILiveCallRegistry
{
    Task RecordStateAsync(LiveCallRecord call, CancellationToken ct);
    Task RemoveAsync(string tenantId, string callId, string reason, CancellationToken ct);
    Task UpdateMetricsAsync(string tenantId, int? avgHandleSec, int? slaPercent, int? slaTargetPercent, CancellationToken ct);
    Task<LiveCallsSnapshot> GetSnapshotAsync(string tenantId, CancellationToken ct);
}
