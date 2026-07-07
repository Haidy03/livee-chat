using MongoDB.Bson;
using VoiceFlow.Application.Interfaces.WrapUpCodes;
using VoiceFlow.Contracts.WrapUpCodes.Requests;
using VoiceFlow.Contracts.WrapUpCodes.Responses;
using VoiceFlow.Core.Entities.WrapUpCodes;
using VoiceFlow.Core.Interfaces.Repositories.WrapUpCodes;

namespace VoiceFlow.Application.Services.WrapUpCodes;

public sealed class WrapUpCodeService : IWrapUpCodeService
{
    private readonly IWrapUpCodeRepository _repo;
    private readonly IQueueWrapUpCodeRepository _mapRepo;

    public WrapUpCodeService(IWrapUpCodeRepository repo, IQueueWrapUpCodeRepository mapRepo)
    {
        _repo = repo;
        _mapRepo = mapRepo;
    }

    public async Task<IReadOnlyList<WrapUpCodeResponse>> ListAsync(string tenantId, bool activeOnly, CancellationToken ct)
    {
        var items = await _repo.ListAsync(tenantId, activeOnly, ct);
        return items.Select(Map).ToList();
    }

    public async Task<WrapUpCodeResponse> CreateAsync(string tenantId, CreateWrapUpCodeRequest request, CancellationToken ct)
    {
        var label = (request.Label ?? string.Empty).Trim();
        if (label.Length == 0) throw new InvalidOperationException("Label is required.");

        var now = DateTime.UtcNow;
        var entity = new WrapUpCode
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = tenantId,
            Label = label,
            LabelAr = string.IsNullOrWhiteSpace(request.LabelAr) ? null : request.LabelAr!.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "general" : request.Category!.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#64748b" : request.Color!.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _repo.UpsertAsync(entity, ct);
        return Map(entity);
    }

    public async Task<WrapUpCodeResponse> UpdateAsync(string tenantId, string id, UpdateWrapUpCodeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(tenantId, id, ct)
            ?? throw new KeyNotFoundException("Wrap-up code not found.");

        if (request.Label is not null)
        {
            var label = request.Label.Trim();
            if (label.Length == 0) throw new InvalidOperationException("Label cannot be empty.");
            entity.Label = label;
        }
        if (request.LabelAr is not null)
            entity.LabelAr = string.IsNullOrWhiteSpace(request.LabelAr) ? null : request.LabelAr.Trim();
        if (request.Category is not null) entity.Category = request.Category.Trim();
        if (request.Color is not null) entity.Color = request.Color.Trim();
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(entity, ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteAsync(tenantId, id, ct);
        if (deleted) await _mapRepo.DeleteByCodeIdAsync(tenantId, id, ct);
        return deleted;
    }

    public Task<IReadOnlyList<string>> GetQueueCodeIdsAsync(string tenantId, string queueId, CancellationToken ct)
        => _mapRepo.ListCodeIdsAsync(tenantId, queueId, ct);

    public async Task SetQueueCodesAsync(string tenantId, string queueId, IReadOnlyList<string> codeIds, CancellationToken ct)
    {
        var distinct = codeIds.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (distinct.Count > 0)
        {
            var existing = await _repo.GetByIdsAsync(tenantId, distinct, ct);
            if (existing.Count != distinct.Count)
                throw new InvalidOperationException("One or more wrap-up code ids are invalid for this tenant.");
        }
        await _mapRepo.ReplaceForQueueAsync(tenantId, queueId, distinct, ct);
    }

    public async Task<IReadOnlyList<WrapUpCodeResponse>> GetEffectiveForQueueAsync(string tenantId, string queueId, CancellationToken ct)
    {
        var ids = await _mapRepo.ListCodeIdsAsync(tenantId, queueId, ct);
        if (ids.Count == 0)
        {
            var all = await _repo.ListAsync(tenantId, activeOnly: true, ct);
            return all.Select(Map).ToList();
        }
        var mapped = await _repo.GetByIdsAsync(tenantId, ids, ct);
        return mapped.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Label).Select(Map).ToList();
    }

    private static WrapUpCodeResponse Map(WrapUpCode c) => new()
    {
        Id = c.Id,
        TenantId = c.TenantId,
        Label = c.Label,
        LabelAr = c.LabelAr,
        Category = c.Category,
        Color = c.Color,
        IsActive = c.IsActive,
        SortOrder = c.SortOrder,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}
