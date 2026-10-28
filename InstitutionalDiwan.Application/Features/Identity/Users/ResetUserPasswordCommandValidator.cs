using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.NewPassword)
            .Must(password => !string.IsNullOrWhiteSpace(password)).WithErrorCode("Identity.PasswordEmpty")
            .MinimumLength(8).WithErrorCode("Identity.PasswordTooShort")
            .MaximumLength(200);
    }
}
