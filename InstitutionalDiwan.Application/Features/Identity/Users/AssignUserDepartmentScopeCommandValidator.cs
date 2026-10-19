using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

public sealed class AssignUserDepartmentScopeCommandValidator
    : AbstractValidator<AssignUserDepartmentScopeCommand>
{
    public AssignUserDepartmentScopeCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.DepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate!.Value)
            .When(command => command.StartDate.HasValue && command.EndDate.HasValue)
            .WithErrorCode("Identity.InvalidDateRange");
    }
}
