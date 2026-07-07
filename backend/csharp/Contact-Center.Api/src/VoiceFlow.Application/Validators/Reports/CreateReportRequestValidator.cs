using FluentValidation;
using VoiceFlow.Contracts.Reports;

namespace VoiceFlow.Application.Validators.Reports
{
    public sealed class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
    {
        public CreateReportRequestValidator()
        {
            RuleFor(x => x.Name.En).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Name.Ar).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.Category).NotEmpty();
            RuleFor(x => x.Type).NotEmpty();
        }
    }
}
