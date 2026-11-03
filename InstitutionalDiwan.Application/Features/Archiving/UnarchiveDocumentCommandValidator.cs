using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Application.Features.Archiving;

public sealed class UnarchiveDocumentCommandValidator : AbstractValidator<UnarchiveDocumentCommand>
{
    public UnarchiveDocumentCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithErrorCode("Archiving.UnarchiveReasonEmpty")
            .MaximumLength(DocumentArchiveEvent.MaxReasonLength)
            .WithErrorCode("Archiving.ReasonTooLong");
    }
}
