using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

public sealed class CreateSavedSearchCommandValidator : AbstractValidator<CreateSavedSearchCommand>
{
    public CreateSavedSearchCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode("Archiving.SearchNameEmpty")
            .MaximumLength(SavedSearch.MaxNameLength)
            .WithErrorCode("Archiving.SearchNameTooLong");

        RuleFor(command => command.CriteriaJson)
            .Must(criteria => !string.IsNullOrWhiteSpace(criteria))
            .WithErrorCode("Archiving.CriteriaJsonEmpty")
            .MaximumLength(SavedSearch.MaxCriteriaLength)
            .WithErrorCode("Archiving.CriteriaInvalid");
    }
}
