using FluentValidation;

using VoiceFlow.Contracts.Reports;

namespace VoiceFlow.Application.Validators.Reports
{
    public sealed class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
    {
        public UpdateReportRequestValidator()
        {
            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name!.En).NotEmpty().MaximumLength(200);
                RuleFor(x => x.Name!.Ar).NotEmpty().MaximumLength(200);
            });
        }
    }
}
