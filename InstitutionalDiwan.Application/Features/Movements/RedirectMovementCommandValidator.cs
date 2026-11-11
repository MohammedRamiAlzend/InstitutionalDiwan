using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Movements;

public sealed class RedirectMovementCommandValidator : AbstractValidator<RedirectMovementCommand>
{
    public RedirectMovementCommandValidator()
    {
        RuleFor(command => command.MovementId)
            .NotEmpty().WithErrorCode("Movements.MovementNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.NewDestinationDepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.MovementTypeId)
            .GreaterThan(0).WithErrorCode("Lookup.InvalidId")
            .When(command => command.MovementTypeId.HasValue);

        RuleFor(command => command.Instructions)
            .MaximumLength(1000);
    }
}
