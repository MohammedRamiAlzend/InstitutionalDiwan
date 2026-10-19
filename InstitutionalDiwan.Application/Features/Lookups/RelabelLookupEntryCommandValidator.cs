using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Lookups;

public sealed class RelabelLookupEntryCommandValidator : AbstractValidator<RelabelLookupEntryCommand>
{
    public RelabelLookupEntryCommandValidator()
    {
        RuleFor(command => command.Lookup)
            .Must(lookup => !string.IsNullOrWhiteSpace(lookup))
            .WithErrorCode("Lookup.UnknownCatalog");

        RuleFor(command => command.Id)
            .GreaterThan(0).WithErrorCode("Lookup.InvalidId");

        RuleFor(command => command.Desc)
            .Must(desc => !string.IsNullOrWhiteSpace(desc)).WithErrorCode("Lookup.LabelEmpty")
            .MaximumLength(150);

        RuleFor(command => command.DescAr)
            .Must(descAr => !string.IsNullOrWhiteSpace(descAr)).WithErrorCode("Lookup.LabelEmpty")
            .MaximumLength(150);
    }
}
