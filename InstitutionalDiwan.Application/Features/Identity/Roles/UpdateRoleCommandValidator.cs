using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty().WithErrorCode("Identity.RoleNotFound");

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Identity.RoleNameEmpty")
            .MaximumLength(150);

        RuleFor(command => command.NameEn).MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
