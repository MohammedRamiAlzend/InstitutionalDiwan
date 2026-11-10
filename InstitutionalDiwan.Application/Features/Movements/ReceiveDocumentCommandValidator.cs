using FluentValidation;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Application.Features.Movements;

public sealed class ReceiveDocumentCommandValidator : AbstractValidator<ReceiveDocumentCommand>
{
    public ReceiveDocumentCommandValidator(IClock clock)
    {
        RuleFor(command => command.MovementId)
            .NotEmpty().WithErrorCode("Movements.MovementNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Notes)
            .MaximumLength(1000);

        RuleFor(command => command.RegistryNumberText)
            .MaximumLength(100);

        RuleFor(command => command.RegistryNumberText)
            .NotEmpty().WithErrorCode("Numbering.DepartmentNumberRequired")
            .When(command => command.IssueNewNumber);

        RuleFor(command => command.RegistryDate)
            .Must(date => date is null
                || (date.Value.Year is >= 1900 and <= 2200 && !RegistryDateRules.IsFuture(date.Value, clock)))
            .WithErrorCode("Numbering.RegistryDateInvalid");
    }
}
