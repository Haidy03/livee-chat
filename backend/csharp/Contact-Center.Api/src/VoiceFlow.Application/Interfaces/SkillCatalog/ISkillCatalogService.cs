using VoiceFlow.Api.SkillCatalog.Requests;
using VoiceFlow.Api.SkillCatalog.Responses;
using VoiceFlow.Contracts.SkillCatalog.Requests;

namespace VoiceFlow.Application.Interfaces.SkillCatalog;

public interface ISkillCatalogService
{
    Task<IReadOnlyList<SkillCategoryResponse>> ListAsync(string tenantId, CancellationToken ct);
    Task<IReadOnlyList<SkillCategoryResponse>> ReplaceAllAsync(string tenantId, SaveSkillCatalogRequest request, CancellationToken ct);
    Task<SkillCategoryResponse> CreateCategoryAsync(string tenantId, UpsertSkillCategoryRequest request, CancellationToken ct);
    Task<SkillCategoryResponse> UpdateCategoryAsync(string tenantId, string categoryId, UpsertSkillCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteCategoryAsync(string tenantId, string categoryId, CancellationToken ct);
    Task<SkillOptionResponse> AddOptionAsync(string tenantId, string categoryId, UpsertSkillOptionRequest request, CancellationToken ct);
    Task<SkillOptionResponse> UpdateOptionAsync(string tenantId, string categoryId, string optionId, UpsertSkillOptionRequest request, CancellationToken ct);
    Task<bool> DeleteOptionAsync(string tenantId, string categoryId, string optionId, CancellationToken ct);
}
