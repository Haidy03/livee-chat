using VoiceFlow.Core.Entities.SkillCatalog;
namespace VoiceFlow.Core.Interfaces.Repositories.SkillCatalog;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<SkillCategory>> ListAsync(string tenantId, CancellationToken ct);
    Task<SkillCategory?> GetAsync(string tenantId, string categoryId, CancellationToken ct);
    Task ReplaceAllAsync(string tenantId, IReadOnlyList<SkillCategory> categories, CancellationToken ct);
    Task UpsertAsync(SkillCategory category, CancellationToken ct);
    Task<bool> DeleteAsync(string tenantId, string categoryId, CancellationToken ct);
}
