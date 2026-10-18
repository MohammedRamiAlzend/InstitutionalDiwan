using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound");

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Department.NameEmpty")
            .MaximumLength(200);

        RuleFor(command => command.NameEn).MaximumLength(200);
    }
}
