using VoiceFlow.Core.Entities.Surveys;

namespace VoiceFlow.Contracts.Surveys
{
    public record SurveyWebhookAnswer(
    string QuestionId,
    string? QuestionText,
    string? Type,
    string? Answer,
    string? AnsweredAt);

    public record SurveyWebhookPayload(
        string? Event,
        string SurveyId,
        string? SurveyName,
        string? CallId,
        string? PhoneNumber,
        string? StartedAt,
        string? EndedAt,
        int? DurationSeconds,
        string? CompletionStatus,
        string? Language,
        Dictionary<string, string>? CustomFields,
        Dictionary<string, string>? PassedVariables,
        List<SurveyWebhookAnswer> Answers);

    public record SurveyWebhookResult(SurveyResponse Response, bool Duplicate);
}
