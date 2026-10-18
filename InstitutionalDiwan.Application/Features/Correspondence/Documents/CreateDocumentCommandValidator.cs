using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("Correspondence.TitleEmpty")
            .MaximumLength(500).WithErrorCode("Correspondence.TitleTooLong");

        RuleFor(command => command.DocumentTypeId)
            .GreaterThan(0).WithErrorCode("Correspondence.TypeCodeEmpty");

        RuleFor(command => command.DirectionId)
            .Must(directionId => directionId is DocumentDirection.InternalId
                or DocumentDirection.IncomingId
                or DocumentDirection.OutgoingId)
            .WithErrorCode("Correspondence.DirectionInvalid");

        RuleFor(command => command.OfficialNumberText)
            .Must(number => !string.IsNullOrWhiteSpace(number)).WithErrorCode("Numbering.OfficialNumberEmpty")
            .MaximumLength(100).WithErrorCode("Numbering.OfficialNumberEmpty");

        RuleFor(command => command.NumberingYear)
            .InclusiveBetween(1900, 2200).WithErrorCode("Numbering.YearInvalid");

        RuleFor(command => command.ConfidentialityLevelId)
            .GreaterThan(0).When(command => command.ConfidentialityLevelId.HasValue);

        RuleFor(command => command.PriorityLevelId)
            .GreaterThan(0).When(command => command.PriorityLevelId.HasValue);
    }
}
