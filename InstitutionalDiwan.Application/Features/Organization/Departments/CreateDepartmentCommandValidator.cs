using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.Code)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode("Department.CodeEmpty")
            .MaximumLength(50).WithErrorCode("Department.CodeTooLong");

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Department.NameEmpty")
            .MaximumLength(200);

        RuleFor(command => command.DepartmentTypeId)
            .GreaterThan(0).WithErrorCode("Department.TypeEmpty");

        RuleFor(command => command.NameEn).MaximumLength(200);
        RuleFor(command => command.Notes).MaximumLength(500);
    }
}
