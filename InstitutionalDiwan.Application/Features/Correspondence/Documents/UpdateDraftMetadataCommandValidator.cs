using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class UpdateDraftMetadataCommandValidator
    : AbstractValidator<UpdateDraftMetadataCommand>
{
    public UpdateDraftMetadataCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("Correspondence.TitleEmpty")
            .MaximumLength(500).WithErrorCode("Correspondence.TitleTooLong");

        RuleFor(command => command.SubjectSummary)
            .MaximumLength(2000);

        RuleFor(command => command.ConfidentialityLevelId)
            .GreaterThan(0).When(command => command.ConfidentialityLevelId.HasValue);

        RuleFor(command => command.PriorityLevelId)
            .GreaterThan(0).When(command => command.PriorityLevelId.HasValue);
    }
}
