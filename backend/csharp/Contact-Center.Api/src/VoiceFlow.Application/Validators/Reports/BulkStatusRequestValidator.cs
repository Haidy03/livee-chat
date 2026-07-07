using FluentValidation;
using VoiceFlow.Contracts.Reports;
namespace VoiceFlow.Application.Validators.Reports;
public sealed class BulkStatusRequestValidator : AbstractValidator<BulkStatusRequest>
{
    public BulkStatusRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}
