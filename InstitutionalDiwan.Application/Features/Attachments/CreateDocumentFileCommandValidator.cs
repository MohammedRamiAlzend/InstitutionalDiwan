using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed class CreateDocumentFileCommandValidator : AbstractValidator<CreateDocumentFileCommand>
{
    public CreateDocumentFileCommandValidator()
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
    }
}
