using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class CancelDocumentCommandValidator : AbstractValidator<CancelDocumentCommand>
{
    public CancelDocumentCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithErrorCode("Correspondence.CancelReasonEmpty")
            .MaximumLength(1000);
    }
}
