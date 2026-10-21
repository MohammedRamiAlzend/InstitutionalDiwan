using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Movements;

public sealed class CreateMovementCommandValidator : AbstractValidator<CreateMovementCommand>
{
    public CreateMovementCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.DestinationDepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.MovementTypeId)
            .GreaterThan(0).WithErrorCode("Lookup.InvalidId");

        RuleFor(command => command.Instructions)
            .MaximumLength(1000);
    }
}
