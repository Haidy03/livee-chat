
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;


namespace VoiceFlow.Core.Interfaces.Repositories.Surveys
{
    public interface ISurveyRepository
    {
        Task<IReadOnlyList<Survey>> ListAsync(string tenantId, string? search, SurveyStatus? status, SurveyLanguage? language, CancellationToken ct);
        Task<Survey?> GetAsync(string tenantId, string id, CancellationToken ct);
        Task AddAsync(Survey survey, CancellationToken ct);
        Task UpdateSurveyAsync(Survey survey, CancellationToken ct);
        Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct);
        Task<Survey?> GetSurveyByIdAsync(string id, CancellationToken ct);
    }
}
