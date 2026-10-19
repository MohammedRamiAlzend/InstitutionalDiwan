using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

public sealed class CreateExternalOrganizationCommandValidator
    : AbstractValidator<CreateExternalOrganizationCommand>
{
    public CreateExternalOrganizationCommandValidator()
    {
        RuleFor(command => command.Code)
            .Must(code => !string.IsNullOrWhiteSpace(code))
            .WithErrorCode("Correspondence.ExternalOrgCodeEmpty")
            .MaximumLength(50).WithErrorCode("Correspondence.ExternalOrgCodeTooLong");

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
