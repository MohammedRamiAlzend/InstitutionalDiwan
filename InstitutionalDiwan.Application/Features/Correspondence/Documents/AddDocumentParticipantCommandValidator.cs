using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class AddDocumentParticipantCommandValidator
    : AbstractValidator<AddDocumentParticipantCommand>
{
    public AddDocumentParticipantCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.RoleId)
            .GreaterThan(0).WithErrorCode("Correspondence.ParticipantRoleEmpty");

        RuleFor(command => command.PersonName)
            .MaximumLength(200);

        RuleFor(command => command.Notes)
            .MaximumLength(1000);

        RuleFor(command => command)
            .Must(command =>
            {
                var provided = (command.ExternalOrganizationId.HasValue ? 1 : 0) +
                               (command.InternalDepartmentId.HasValue ? 1 : 0) +
                               (!string.IsNullOrWhiteSpace(command.PersonName) ? 1 : 0);

                return provided == 1;
            })
            .WithErrorCode("Correspondence.ParticipantTargetInvalid");
    }
}
