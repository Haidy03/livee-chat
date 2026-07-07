
using FluentValidation;
using VoiceFlow.Contracts.Surveys;

namespace VoiceFlow.Application.Validators.Serveys
{
    public sealed class SurveyCreateRequestValidator : AbstractValidator<SurveyCreateRequest>
    {
        public SurveyCreateRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.WebhookUrl).NotEmpty().Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithMessage("WebhookUrl must be an absolute URI.");
            RuleFor(x => x.MaxRetries).InclusiveBetween(0, 10);
            RuleFor(x => x.InputTimeoutSec).InclusiveBetween(1, 60);
        }
    }
}
