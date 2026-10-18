using FluentValidation;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    private static readonly string[] AllowedDirections = { "INTERNAL", "INCOMING", "OUTGOING" };

    public CreateDocumentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithErrorCode("Identity.UserIdEmpty");

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("Correspondence.TitleEmpty")
            .MaximumLength(500).WithErrorCode("Correspondence.TitleTooLong");

        RuleFor(command => command.DocumentTypeCode)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode("Correspondence.TypeCodeEmpty")
            .MaximumLength(50);

        RuleFor(command => command.Direction)
            .Must(direction => direction is not null &&
                               AllowedDirections.Contains(direction.Trim().ToUpperInvariant()))
            .WithErrorCode("Correspondence.DirectionInvalid");

        RuleFor(command => command.OfficialNumberText)
            .Must(number => !string.IsNullOrWhiteSpace(number)).WithErrorCode("Numbering.OfficialNumberEmpty")
            .MaximumLength(100).WithErrorCode("Numbering.OfficialNumberEmpty");

        RuleFor(command => command.NumberingYear)
            .InclusiveBetween(1900, 2200).WithErrorCode("Numbering.YearInvalid");

        RuleFor(command => command.ConfidentialityLevel).MaximumLength(50);
        RuleFor(command => command.PriorityLevel).MaximumLength(50);
    }
}
