using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

public sealed class UpdateExternalOrganizationCommandValidator
    : AbstractValidator<UpdateExternalOrganizationCommand>
{
    public UpdateExternalOrganizationCommandValidator()
    {
        RuleFor(command => command.ExternalOrganizationId)
            .NotEmpty().WithErrorCode("Correspondence.ExternalOrgNotFound");

        RuleFor(command => command.NameAr)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode("Correspondence.ExternalOrgNameEmpty")
            .MaximumLength(200);

        RuleFor(command => command.OrganizationTypeId)
            .GreaterThan(0).WithErrorCode("Lookup.InvalidId");

        RuleFor(command => command.NameEn)
            .MaximumLength(200);

        RuleFor(command => command.ContactInfo)
            .MaximumLength(254)
            .Must(ExternalOrganizationContact.IsValid)
            .WithErrorCode("Correspondence.ExternalOrgContactInvalid");
    }
}
