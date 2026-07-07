using CtiBackend.Models.Ami;
using CtiBackend.Services.Ami;

namespace CtiBackend.Services.QueueMonitoring;

public interface IQueueMonitoringEventHandler
{
    Task HandleAsync(AmiEventEnvelope amiEvent, AmiConnectionContext context, CancellationToken ct);
}
