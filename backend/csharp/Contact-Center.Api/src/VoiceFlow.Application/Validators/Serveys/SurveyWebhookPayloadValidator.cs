using FluentValidation;
using VoiceFlow.Contracts.Surveys;


namespace VoiceFlow.Application.Validators.Serveys
{
    public sealed class SurveyWebhookPayloadValidator : AbstractValidator<SurveyWebhookPayload>
    {
        public SurveyWebhookPayloadValidator()
        {
            RuleFor(x => x.SurveyId).NotEmpty();
            RuleFor(x => x.CallId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Answers).NotNull();
            RuleForEach(x => x.Answers).ChildRules(a =>
            {
                a.RuleFor(y => y.QuestionId).NotEmpty();
            });
        }
    }
}
