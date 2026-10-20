using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Movements;

public sealed class ReceiveDocumentCommandValidator : AbstractValidator<ReceiveDocumentCommand>
{
    public ReceiveDocumentCommandValidator()
    {
        RuleFor(command => command.MovementId)
            .NotEmpty().WithErrorCode("Movements.MovementNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Notes)
            .MaximumLength(1000);
    }
}
