
using VoiceFlow.Core.Entities.Surveys;

namespace VoiceFlow.Core.Interfaces.Repositories.Surveys
{
    public interface ISurveyResponseRepository
    {
        Task<IReadOnlyList<SurveyResponse>> ListAsync(string tenantId, string surveyId, int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
        Task<SurveyResponse?> GetByCallIdAsync(string surveyId, string callId, CancellationToken ct);
        Task InsertSurveyResponseAsync(SurveyResponse response, CancellationToken ct);

    }
}
