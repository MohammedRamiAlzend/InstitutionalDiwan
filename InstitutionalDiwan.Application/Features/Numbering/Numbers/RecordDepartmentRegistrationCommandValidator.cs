using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

public sealed class RecordDepartmentRegistrationCommandValidator
    : AbstractValidator<RecordDepartmentRegistrationCommand>
{
    public RecordDepartmentRegistrationCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.DepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.RegistryNumberText)
            .NotEmpty().WithErrorCode("Numbering.DepartmentNumberRequired")
            .MaximumLength(100);

        RuleFor(command => command.RegistryDate)
            .Must(date => date != default && date.Year is >= 1900 and <= 2200)
            .WithErrorCode("Numbering.RegistryDateInvalid");

        RuleFor(command => command.Notes)
            .MaximumLength(1000);
    }
}
