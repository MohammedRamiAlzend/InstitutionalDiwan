using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

public sealed class GrantRolePermissionCommandValidator : AbstractValidator<GrantRolePermissionCommand>
{
    public GrantRolePermissionCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty().WithErrorCode("Identity.RoleNotFound");

        RuleFor(command => command.PermissionCode)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode("Identity.PermissionCodeEmpty")
            .MaximumLength(120);
    }
}
