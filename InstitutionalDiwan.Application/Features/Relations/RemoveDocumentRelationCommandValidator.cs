using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Application.Features.Relations;

public sealed class RemoveDocumentRelationCommandValidator : AbstractValidator<RemoveDocumentRelationCommand>
{
    public RemoveDocumentRelationCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.RelationId)
            .NotEmpty().WithErrorCode("Relations.RelationNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithErrorCode("Relations.ReasonRequired")
            .MaximumLength(DocumentRelation.MaxReasonLength)
            .WithErrorCode("Relations.ReasonTooLong");
    }
}
