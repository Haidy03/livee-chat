using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;

namespace VoiceFlow.Contracts.Surveys
{
    public record SurveyCreateRequest(
    string Name, string Description, SurveyLanguage Language, TtsVoice TtsVoice,
    string WebhookUrl, string? WebhookSecret,
    int MaxRetries, int InputTimeoutSec, Abandonment? Abandonment, SurveyStatus Status,
    List<Question> Questions, List<Guid> UsedInFlowIds,
     SoundFileInfo? welcomeSound, SoundFileInfo? completionSound);
}


