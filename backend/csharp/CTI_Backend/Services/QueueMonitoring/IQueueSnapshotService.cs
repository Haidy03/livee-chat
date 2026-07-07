using CtiBackend.Models.Ami;
using CtiBackend.Services.Ami;

namespace CtiBackend.Services.QueueMonitoring;

public interface IQueueSnapshotService
{
    Task<string> RequestFullSnapshotAsync(AmiConnectionContext ctx, CancellationToken ct);
    Task HandleSnapshotEventAsync(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct);
    bool IsTracking(string actionId);
}
