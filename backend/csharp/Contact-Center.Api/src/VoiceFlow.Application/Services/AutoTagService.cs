using System.Text.Json;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.AutoTags;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;
using AutoTagEntity = VoiceFlow.Core.Entities.AutoTag;

namespace VoiceFlow.Application.Services;

public sealed class AutoTagService : IAutoTagService
{
    private readonly IAutoTagRepository _repo;

    public AutoTagService(IAutoTagRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<AutoTagResponse>>> GetAutoTagsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _repo.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(rows.Select(Map));
    }

    public async Task<Result<AutoTagResponse>> CreateAutoTagAsync(string tenantId, string userId, CreateAutoTagRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new AutoTagEntity
        {
            TenantId = tenantId,
            UserId = userId,
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Prompt = request.Prompt ?? string.Empty,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color,
            TagId = string.IsNullOrWhiteSpace(request.TagId) ? null : request.TagId,
            Active = request.Active
        };
        await _repo.InsertAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<Result<AutoTagResponse>> UpdateAutoTagAsync(string id, string tenantId, JsonElement patch, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.TenantId != tenantId)
            return Result.Failure<AutoTagResponse>(Error.NotFound("AutoTag", id));

        if (patch.ValueKind == JsonValueKind.Object)
        {
            if (TryGet(patch, "title", out var p) && p.ValueKind == JsonValueKind.String)
                entity.Title = p.GetString() ?? entity.Title;
            if (TryGet(patch, "description", out p) && p.ValueKind == JsonValueKind.String)
                entity.Description = p.GetString() ?? string.Empty;
            if (TryGet(patch, "prompt", out p) && p.ValueKind == JsonValueKind.String)
                entity.Prompt = p.GetString() ?? string.Empty;
            if (TryGet(patch, "color", out p) && p.ValueKind == JsonValueKind.String)
                entity.Color = p.GetString() ?? entity.Color;
            if (TryGet(patch, "tagId", out p))
                entity.TagId = p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            if (TryGet(patch, "active", out p) && (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
                entity.Active = p.GetBoolean();
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<Result> DeleteAutoTagAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.TenantId != tenantId)
            return Result.Failure(Error.NotFound("AutoTag", id));
        await _repo.DeleteAsync(id, cancellationToken);
        return Result.Success();
    }

    private static bool TryGet(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static AutoTagResponse Map(AutoTagEntity t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Prompt = t.Prompt,
        Color = t.Color,
        TagId = t.TagId,
        Active = t.Active
    };
}
