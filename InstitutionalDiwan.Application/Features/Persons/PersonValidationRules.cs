using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Persons;

namespace InstitutionalDiwan.Application.Features.Persons;

/// <summary>Limits on one person submitted with a document, shared by every flow that accepts them.</summary>
public sealed class DocumentPersonInputValidator : AbstractValidator<DocumentPersonInput>
{
    /// <summary>The most people one request can name on a document.</summary>
    public const int MaxPersonsPerRequest = 50;

    public DocumentPersonInputValidator()
    {
        RuleFor(input => input)
            .Must(input => input.PersonId.HasValue != !string.IsNullOrWhiteSpace(input.FullName))
            .WithErrorCode("Persons.ReferenceInvalid");

        RuleFor(input => input.FullName)
            .MaximumLength(Person.MaxFullNameLength).WithErrorCode("Persons.ValueTooLong");

        RuleFor(input => input.RoleId)
            .GreaterThan(0).WithErrorCode("Persons.RoleNotFound")
            .When(input => input.RoleId.HasValue);
    }
}
