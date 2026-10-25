using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed class CreateDocumentFileWithVersionCommandValidator
    : AbstractValidator<CreateDocumentFileWithVersionCommand>
{
    public CreateDocumentFileWithVersionCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.FileTypeId)
            .GreaterThan(0).WithErrorCode("Attachments.FileTypeEmpty");

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("Attachments.TitleEmpty")
            .MaximumLength(300).WithErrorCode("Attachments.TitleTooLong");

        RuleFor(command => command.OriginalFileName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Attachments.FileEmpty");

        RuleFor(command => command.FileSizeBytes)
            .GreaterThan(0).WithErrorCode("Attachments.FileEmpty");

        RuleFor(command => command.ChangeNotes)
            .MaximumLength(500).WithErrorCode("Attachments.ChangeNotesTooLong");
    }
}
