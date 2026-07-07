using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Queues;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly IQueueRepository _repo;

    public QueueService(IQueueRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<QueueResponse>>> GetQueuesAsync(string tenantId, CancellationToken ct = default)
    {
        var items = await _repo.GetByTenantAsync(tenantId, ct);
        return Result.Success(items.Select(QueueMapping.ToResponse));
    }

    public async Task<Result<QueueResponse>> GetQueueAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var q = await _repo.GetByIdAsync(id, ct);
        if (q is null || q.TenantId != tenantId)
            return Result.Failure<QueueResponse>(Error.NotFound("Queue", id));
        return q.ToResponse();
    }

    public async Task<Result<QueueResponse>> CreateQueueAsync(string tenantId, CreateQueueRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<QueueResponse>(Error.Validation("Name", "Name is required."));
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result.Failure<QueueResponse>(Error.Validation("Code", "Code is required."));
        if (await _repo.ExistsByCodeAsync(tenantId, request.Code, null, ct))
            return Result.Failure<QueueResponse>(Error.Conflict("Queue", $"A queue with code '{request.Code}' already exists."));

        var entity = request.ToEntity();
        entity.TenantId = tenantId;
        await _repo.InsertAsync(entity, ct);
        return entity.ToResponse();
    }

    public async Task<Result<QueueResponse>> UpdateQueueAsync(string id, string tenantId, UpdateQueueRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null || existing.TenantId != tenantId)
            return Result.Failure<QueueResponse>(Error.NotFound("Queue", id));

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<QueueResponse>(Error.Validation("Name", "Name is required."));
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result.Failure<QueueResponse>(Error.Validation("Code", "Code is required."));
        if (await _repo.ExistsByCodeAsync(tenantId, request.Code, id, ct))
            return Result.Failure<QueueResponse>(Error.Conflict("Queue", $"A queue with code '{request.Code}' already exists."));

        request.ApplyTo(existing);
        await _repo.UpdateAsync(existing, ct);
        return existing.ToResponse();
    }

    public async Task<Result> DeleteQueueAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null || existing.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Queue", id));

        await _repo.DeleteAsync(id, ct);
        return Result.Success();
    }

    public async Task<Result<QueueResponse>> DuplicateQueueAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var src = await _repo.GetByIdAsync(id, ct);
        if (src is null || src.TenantId != tenantId)
            return Result.Failure<QueueResponse>(Error.NotFound("Queue", id));

        var copy = src.CloneForDuplicate();
        // ensure unique code; append suffix until free
        var baseCode = copy.Code;
        var attempt = 0;
        while (await _repo.ExistsByCodeAsync(tenantId, copy.Code, null, ct))
        {
            attempt++;
            copy.Code = $"{baseCode}_{attempt}";
        }
        await _repo.InsertAsync(copy, ct);
        return copy.ToResponse();
    }

    public async Task<Result<QueueResponse>> ToggleStatusAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null || existing.TenantId != tenantId)
            return Result.Failure<QueueResponse>(Error.NotFound("Queue", id));

        existing.Status = existing.Status == "active" ? "inactive" : "active";
        await _repo.UpdateAsync(existing, ct);
        return existing.ToResponse();
    }
}
