using MongoDB.Bson;
using VoiceFlow.Api.SkillCatalog.Requests;
using VoiceFlow.Api.SkillCatalog.Responses;
using VoiceFlow.Application.Interfaces.SkillCatalog;
using VoiceFlow.Contracts.SkillCatalog.Requests;
using VoiceFlow.Core.Entities.SkillCatalog;
using VoiceFlow.Core.Interfaces.Repositories.SkillCatalog;

namespace VoiceFlow.Application.Services.SkillCatalog;

public sealed class SkillCatalogService : ISkillCatalogService
{
    private readonly ISkillCatalogRepository _repo;

    public SkillCatalogService(ISkillCatalogRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SkillCategoryResponse>> ListAsync(string tenantId, CancellationToken ct)
    {
        var items = await _repo.ListAsync(tenantId, ct);
        return items.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SkillCategoryResponse>> ReplaceAllAsync(
        string tenantId, SaveSkillCatalogRequest request, CancellationToken ct)
    {
        if (request?.Categories is null) throw new ArgumentException("Categories required");

        // Validate uniqueness
        var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in request.Categories)
        {
            var name = (c.Name ?? string.Empty).Trim();
            if (name.Length == 0) throw new InvalidOperationException("Category name is required.");
            if (!nameSet.Add(name)) throw new InvalidOperationException($"Duplicate category name: {name}");

            var labelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in c.Options)
            {
                var label = (o.Label ?? string.Empty).Trim();
                if (label.Length == 0) throw new InvalidOperationException("Option label is required.");
                if (!labelSet.Add(label))
                    throw new InvalidOperationException($"Duplicate option '{label}' in category '{name}'.");
            }
        }

        // Preserve UsageCount from existing data when ids match
        var existing = (await _repo.ListAsync(tenantId, ct))
            .SelectMany(c => c.Options.Select(o => (o.Id, o.UsageCount)))
            .ToDictionary(x => x.Id, x => x.UsageCount, StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        var categories = request.Categories
            .Select((c, idx) => new SkillCategory
            {
                Id = string.IsNullOrWhiteSpace(c.Id) ? ObjectId.GenerateNewId().ToString() : c.Id!,
                TenantId = tenantId,
                Name = c.Name.Trim(),
                SortOrder = idx,
                Active = c.Active,
                CreatedAt = now,
                UpdatedAt = now,
                Options = c.Options.Select((o, oi) => new SkillOption
                {
                    Id = string.IsNullOrWhiteSpace(o.Id) ? ObjectId.GenerateNewId().ToString() : o.Id!,
                    Label = o.Label.Trim(),
                    SortOrder = oi,
                    Active = o.Active,
                    UsageCount = o.UsageCount
                        ?? (o.Id != null && existing.TryGetValue(o.Id, out var u) ? u : 0),
                }).ToList(),
            })
            .ToList();

        await _repo.ReplaceAllAsync(tenantId, categories, ct);
        return categories.Select(Map).ToList();
    }

    public async Task<SkillCategoryResponse> CreateCategoryAsync(
        string tenantId, UpsertSkillCategoryRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new InvalidOperationException("Category name is required.");

        var existing = await _repo.ListAsync(tenantId, ct);
        if (existing.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A category with this name already exists.");

        var entity = new SkillCategory
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = tenantId,
            Name = name,
            SortOrder = request.SortOrder == 0 ? existing.Count : request.SortOrder,
            Active = request.Active,
        };
        await _repo.UpsertAsync(entity, ct);
        return Map(entity);
    }

    public async Task<SkillCategoryResponse> UpdateCategoryAsync(
        string tenantId, string categoryId, UpsertSkillCategoryRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(tenantId, categoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new InvalidOperationException("Category name is required.");

        var existing = await _repo.ListAsync(tenantId, ct);
        if (existing.Any(c => c.Id != categoryId
                              && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A category with this name already exists.");

        entity.Name = name;
        entity.SortOrder = request.SortOrder;
        entity.Active = request.Active;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(entity, ct);
        return Map(entity);
    }

    public Task<bool> DeleteCategoryAsync(string tenantId, string categoryId, CancellationToken ct) =>
        _repo.DeleteAsync(tenantId, categoryId, ct);

    public async Task<SkillOptionResponse> AddOptionAsync(
        string tenantId, string categoryId, UpsertSkillOptionRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(tenantId, categoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        var label = (request.Label ?? string.Empty).Trim();
        if (label.Length == 0) throw new InvalidOperationException("Option label is required.");
        if (entity.Options.Any(o => string.Equals(o.Label, label, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This option already exists in this category.");

        var opt = new SkillOption
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Label = label,
            SortOrder = entity.Options.Count,
            Active = request.Active,
        };
        entity.Options.Add(opt);
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(entity, ct);
        return MapOption(opt);
    }

    public async Task<SkillOptionResponse> UpdateOptionAsync(
        string tenantId, string categoryId, string optionId,
        UpsertSkillOptionRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(tenantId, categoryId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        var opt = entity.Options.FirstOrDefault(o => o.Id == optionId)
            ?? throw new KeyNotFoundException("Option not found.");
        var label = (request.Label ?? string.Empty).Trim();
        if (label.Length == 0) throw new InvalidOperationException("Option label is required.");
        if (entity.Options.Any(o => o.Id != optionId
                                    && string.Equals(o.Label, label, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Another option in this category already uses this label.");

        opt.Label = label;
        opt.SortOrder = request.SortOrder;
        opt.Active = request.Active;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(entity, ct);
        return MapOption(opt);
    }

    public async Task<bool> DeleteOptionAsync(string tenantId, string categoryId, string optionId, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(tenantId, categoryId, ct);
        if (entity is null) return false;
        var removed = entity.Options.RemoveAll(o => o.Id == optionId) > 0;
        if (!removed) return false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(entity, ct);
        return true;
    }

    private static SkillCategoryResponse Map(SkillCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        SortOrder = c.SortOrder,
        Active = c.Active,
        Options = c.Options.OrderBy(o => o.SortOrder).Select(MapOption).ToList(),
    };

    private static SkillOptionResponse MapOption(SkillOption o) => new()
    {
        Id = o.Id,
        Label = o.Label,
        SortOrder = o.SortOrder,
        Active = o.Active,
        UsageCount = o.UsageCount,
    };
}
