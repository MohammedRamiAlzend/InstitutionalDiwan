using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

public sealed class RevokeUserRoleCommandValidator : AbstractValidator<RevokeUserRoleCommand>
{
    public RevokeUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserNotFound");

        RuleFor(command => command.RoleId)
            .NotEmpty().WithErrorCode("Identity.RoleNotFound");
    }
}
