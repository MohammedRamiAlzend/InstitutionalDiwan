using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.OldPassword)
            .Must(password => !string.IsNullOrWhiteSpace(password)).WithErrorCode("Identity.PasswordEmpty")
            .MaximumLength(200);

        RuleFor(command => command.NewPassword)
            .Must(password => !string.IsNullOrWhiteSpace(password)).WithErrorCode("Identity.PasswordEmpty")
            .MinimumLength(8).WithErrorCode("Identity.PasswordTooShort")
            .MaximumLength(200)
            .NotEqual(command => command.OldPassword).WithErrorCode("Identity.NewPasswordSameAsOld");
    }
}
