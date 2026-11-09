using FluentValidation;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Title)
            .MaximumLength(500).WithErrorCode("Correspondence.TitleTooLong");

        RuleFor(command => command.DocumentTypeId)
            .GreaterThan(0).WithErrorCode("Correspondence.TypeCodeEmpty");

        RuleFor(command => command.DirectionId)
            .Must(directionId => directionId is DocumentDirection.InternalId
                or DocumentDirection.IncomingId
                or DocumentDirection.OutgoingId)
            .When(command => command.DirectionId.HasValue)
            .WithErrorCode("Correspondence.DirectionInvalid");

        RuleFor(command => command.DepartmentRegistryNumberText)
            .Must(number => !string.IsNullOrWhiteSpace(number)).WithErrorCode("Numbering.DepartmentNumberRequired")
            .MaximumLength(100).WithErrorCode("Numbering.DepartmentNumberRequired");

        RuleFor(command => command.DepartmentRegistryDate)
            .Must(date => date != default && date.Year is >= 1900 and <= 2200)
            .WithErrorCode("Numbering.RegistryDateInvalid");

        RuleFor(command => command.Movement!.DestinationDepartmentId)
            .NotEmpty().WithErrorCode("Department.NotFound")
            .When(command => command.Movement is not null);

        RuleFor(command => command.Persons)
            .Must(persons => persons is null || persons.Count <= DocumentPersonInputValidator.MaxPersonsPerRequest)
            .WithErrorCode("Persons.ValueTooLong");

        RuleForEach(command => command.Persons).SetValidator(new DocumentPersonInputValidator());

        RuleFor(command => command.ConfidentialityLevelId)
            .GreaterThan(0).When(command => command.ConfidentialityLevelId.HasValue);

        RuleFor(command => command.PriorityLevelId)
            .GreaterThan(0).When(command => command.PriorityLevelId.HasValue);
    }
}
