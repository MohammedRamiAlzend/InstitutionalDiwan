using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserNotFound");

        RuleFor(command => command.PrimaryDepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.ScopeTypeId)
            .InclusiveBetween(1, 4).WithErrorCode("Identity.UserScopeTypeInvalid");
    }
}
