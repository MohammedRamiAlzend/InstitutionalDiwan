using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed class UploadDocumentFileVersionCommandValidator
    : AbstractValidator<UploadDocumentFileVersionCommand>
{
    public UploadDocumentFileVersionCommandValidator()
    {
        RuleFor(command => command.FileId)
            .NotEmpty().WithErrorCode("Attachments.FileNotFound");

        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.OriginalFileName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("Attachments.FileEmpty");

        RuleFor(command => command.FileSizeBytes)
            .GreaterThan(0).WithErrorCode("Attachments.FileEmpty");

        RuleFor(command => command.ChangeNotes)
            .MaximumLength(500).WithErrorCode("Attachments.ChangeNotesTooLong");
    }
}
