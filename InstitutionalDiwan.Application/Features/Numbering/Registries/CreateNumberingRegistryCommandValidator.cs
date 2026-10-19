using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

public sealed class CreateNumberingRegistryCommandValidator
    : AbstractValidator<CreateNumberingRegistryCommand>
{
    public CreateNumberingRegistryCommandValidator()
    {
        RuleFor(command => command.Code)
            .Must(code => !string.IsNullOrWhiteSpace(code))
            .WithErrorCode("Numbering.RegistryCodeEmpty")
            .MaximumLength(50);

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
