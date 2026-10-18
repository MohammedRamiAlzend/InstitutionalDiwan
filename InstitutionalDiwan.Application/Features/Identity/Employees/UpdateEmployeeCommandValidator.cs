using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(command => command.EmployeeId)
            .NotEmpty().WithErrorCode("Identity.EmployeeNotFound");

        RuleFor(command => command.FullNameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Identity.EmployeeNameEmpty")
            .MaximumLength(200);

        RuleFor(command => command.DepartmentId)
            .NotEmpty().WithErrorCode("Identity.EmployeeDepartmentEmpty");

        RuleFor(command => command.EmployeeNumber).MaximumLength(50);
        RuleFor(command => command.FullNameEn).MaximumLength(200);
        RuleFor(command => command.JobTitleAr).MaximumLength(150);
        RuleFor(command => command.Email)
            .MaximumLength(150)
            .EmailAddress().WithErrorCode("Identity.EmployeeEmailInvalid")
            .When(command => !string.IsNullOrWhiteSpace(command.Email));
        RuleFor(command => command.Phone).MaximumLength(50);
    }
}
