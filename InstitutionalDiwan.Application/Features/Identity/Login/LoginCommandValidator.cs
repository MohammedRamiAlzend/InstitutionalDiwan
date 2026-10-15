using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithErrorCode("Identity.UsernameEmpty")
            .MaximumLength(100);

        RuleFor(command => command.Password)
            .NotEmpty().WithErrorCode("Identity.PasswordEmpty")
            .MaximumLength(200);
    }
}
