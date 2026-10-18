using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

public sealed class RevokeRolePermissionCommandValidator : AbstractValidator<RevokeRolePermissionCommand>
{
    public RevokeRolePermissionCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty().WithErrorCode("Identity.RoleNotFound");

        RuleFor(command => command.PermissionCode)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode("Identity.PermissionCodeEmpty")
            .MaximumLength(120);
    }
}
