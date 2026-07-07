using VoiceFlow.Contracts.Queues;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IQueueService
{
    Task<Result<IEnumerable<QueueResponse>>> GetQueuesAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<QueueResponse>> GetQueueAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<QueueResponse>> CreateQueueAsync(string tenantId, CreateQueueRequest request, CancellationToken cancellationToken = default);
    Task<Result<QueueResponse>> UpdateQueueAsync(string id, string tenantId, UpdateQueueRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteQueueAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<QueueResponse>> DuplicateQueueAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<QueueResponse>> ToggleStatusAsync(string id, string tenantId, CancellationToken cancellationToken = default);
}
