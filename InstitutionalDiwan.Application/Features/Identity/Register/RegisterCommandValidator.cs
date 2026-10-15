using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Username)
            .Must(username => !string.IsNullOrWhiteSpace(username)).WithErrorCode("Identity.UsernameEmpty")
            .MinimumLength(3).WithErrorCode("Identity.UsernameTooShort")
            .MaximumLength(100);

        RuleFor(command => command.Password)
            .Must(password => !string.IsNullOrWhiteSpace(password)).WithErrorCode("Identity.PasswordEmpty")
            .MinimumLength(8).WithErrorCode("Identity.PasswordTooShort")
            .MaximumLength(200);
    }
}
