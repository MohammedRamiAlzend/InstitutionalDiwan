using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Code)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode("Identity.RoleCodeEmpty")
            .MaximumLength(100);

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Identity.RoleNameEmpty")
            .MaximumLength(150);

        RuleFor(command => command.NameEn).MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
