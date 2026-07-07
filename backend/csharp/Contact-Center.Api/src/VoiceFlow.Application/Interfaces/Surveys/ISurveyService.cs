
using VoiceFlow.Contracts.Surveys;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;


namespace VoiceFlow.Application.Interfaces.Surveys
{
    public interface ISurveyService
    {
        Task<IReadOnlyList<Survey>> ListAsync(string? search, SurveyStatus? status, SurveyLanguage? language, CancellationToken ct);
        Task<Survey> GetAsync(string id, CancellationToken ct);
        Task<Survey> CreateAsync(SurveyCreateRequest req, CancellationToken ct);
        Task<Survey> UpdateAsync(string id, SurveyUpdateRequest req, CancellationToken ct);
        Task<Survey> SetStatusAsync(string id, SurveyStatus status, CancellationToken ct);
        Task<Survey> DuplicateAsync(string id, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
        Task<IReadOnlyList<SurveyResponse>> ListResponsesAsync(string id, int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
        Task<SurveyWebhookResult> IngestWebhookAsync( SurveyWebhookPayload payload, string rawBody, string? signatureHeader, CancellationToken ct);
    }
}
