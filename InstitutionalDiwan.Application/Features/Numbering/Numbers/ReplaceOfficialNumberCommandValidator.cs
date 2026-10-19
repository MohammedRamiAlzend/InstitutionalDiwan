using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

public sealed class ReplaceOfficialNumberCommandValidator : AbstractValidator<ReplaceOfficialNumberCommand>
{
    public ReplaceOfficialNumberCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.DocumentNumberId)
            .NotEmpty().WithErrorCode("Numbering.NumberNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithErrorCode("Numbering.CancelReasonRequired")
            .MaximumLength(1000);
    }
}
