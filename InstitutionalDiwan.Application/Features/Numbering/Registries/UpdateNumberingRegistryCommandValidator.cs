using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

public sealed class UpdateNumberingRegistryCommandValidator
    : AbstractValidator<UpdateNumberingRegistryCommand>
{
    public UpdateNumberingRegistryCommandValidator()
    {
        RuleFor(command => command.RegistryId)
            .NotEmpty().WithErrorCode("Numbering.RegistryNotFound");

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode("Numbering.RegistryCodeEmpty")
            .MaximumLength(200);

        RuleFor(command => command.FormatPattern)
            .MaximumLength(100);

        RuleFor(command => command.ResetPolicyId)
            .GreaterThanOrEqualTo(0);
    }
}
