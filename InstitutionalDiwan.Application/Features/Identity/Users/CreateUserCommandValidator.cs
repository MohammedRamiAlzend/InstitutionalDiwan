using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Username)
            .Must(username => !string.IsNullOrWhiteSpace(username)).WithErrorCode("Identity.UsernameEmpty")
            .MinimumLength(3).WithErrorCode("Identity.UsernameTooShort")
            .MaximumLength(100);

        RuleFor(command => command.Password)
            .Must(password => !string.IsNullOrWhiteSpace(password)).WithErrorCode("Identity.PasswordEmpty")
            .MinimumLength(8).WithErrorCode("Identity.PasswordTooShort")
            .MaximumLength(200);

        RuleFor(command => command.PrimaryDepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.ScopeTypeId)
            .GreaterThan(0).WithErrorCode("Identity.UserScopeTypeInvalid");
    }
}
