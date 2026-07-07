using FluentValidation;

using VoiceFlow.Contracts.Surveys;

namespace VoiceFlow.Application.Validators.Serveys
{
    public sealed class SurveyUpdateRequestValidator : AbstractValidator<SurveyUpdateRequest>
    {
        public SurveyUpdateRequestValidator()
        {
            When(x => x.Name is not null, () =>
                RuleFor(x => x.Name!).NotEmpty().MaximumLength(200));
            When(x => x.WebhookUrl is not null, () =>
                RuleFor(x => x.WebhookUrl!).Must(u => Uri.TryCreate(u, UriKind.Absolute, out _)));
            When(x => x.MaxRetries is not null, () =>
                RuleFor(x => x.MaxRetries!.Value).InclusiveBetween(0, 10));
            When(x => x.InputTimeoutSec is not null, () =>
                RuleFor(x => x.InputTimeoutSec!.Value).InclusiveBetween(1, 60));
        }
    }

}
