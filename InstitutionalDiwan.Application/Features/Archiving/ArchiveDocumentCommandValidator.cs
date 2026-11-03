using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Application.Features.Archiving;

public sealed class ArchiveDocumentCommandValidator : AbstractValidator<ArchiveDocumentCommand>
{
    public ArchiveDocumentCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.ArchiveLocationId)
            .GreaterThan(0).WithErrorCode("Archiving.LocationNotFound");

        RuleFor(command => command.Reason)
            .MaximumLength(DocumentArchiveEvent.MaxReasonLength).WithErrorCode("Archiving.ReasonTooLong");
    }
}
