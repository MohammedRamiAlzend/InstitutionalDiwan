using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class CreateDraftDocumentCommandValidator : AbstractValidator<CreateDraftDocumentCommand>
{
    public CreateDraftDocumentCommandValidator()
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

        RuleFor(command => command.SubjectSummary)
            .MaximumLength(2000);

        RuleFor(command => command.ExternalReferenceNumber)
            .MaximumLength(100);

        RuleFor(command => command.ConfidentialityLevelId)
            .GreaterThan(0).When(command => command.ConfidentialityLevelId.HasValue);

        RuleFor(command => command.PriorityLevelId)
            .GreaterThan(0).When(command => command.PriorityLevelId.HasValue);
    }
}
