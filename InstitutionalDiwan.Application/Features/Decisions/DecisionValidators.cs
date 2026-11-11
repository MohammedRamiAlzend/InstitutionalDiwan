using FluentValidation;
using InstitutionalDiwan.Domain.Entities.Decisions;

namespace InstitutionalDiwan.Application.Features.Decisions;

public sealed class CreateDecisionTypeCommandValidator : AbstractValidator<CreateDecisionTypeCommand>
{
    public CreateDecisionTypeCommandValidator()
    {
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(DecisionTypeRules.MaxName);
        RuleFor(command => command.EffectId).Must(DecisionEffect.IsKnown).WithErrorCode("Decisions.EffectInvalid");
        RuleFor(command => command.DocumentTypeIds).NotEmpty().WithErrorCode("Decisions.TypeNotAllowedForDocument");
    }
}

public sealed class UpdateDecisionTypeCommandValidator : AbstractValidator<UpdateDecisionTypeCommand>
{
    public UpdateDecisionTypeCommandValidator()
    {
        RuleFor(command => command.DecisionTypeId).NotEmpty().WithErrorCode("Decisions.TypeNotFound");
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(DecisionTypeRules.MaxName);
        RuleFor(command => command.EffectId).Must(DecisionEffect.IsKnown).WithErrorCode("Decisions.EffectInvalid");
        RuleFor(command => command.DocumentTypeIds).NotEmpty().WithErrorCode("Decisions.TypeNotAllowedForDocument");
    }
}

public sealed class IssueDocumentDecisionCommandValidator : AbstractValidator<IssueDocumentDecisionCommand>
{
    public IssueDocumentDecisionCommandValidator()
    {
        RuleFor(command => command.DocumentId).NotEmpty().WithErrorCode("Correspondence.DocumentNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.DecisionTypeId).NotEmpty().WithErrorCode("Decisions.TypeNotFound");
        RuleFor(command => command.DecisionNumber).NotEmpty().MaximumLength(DocumentDecision.MaxNumberLength)
            .WithErrorCode("Decisions.DecisionNumberEmpty");
        RuleFor(command => command.Summary).MaximumLength(DocumentDecision.MaxSummaryLength)
            .WithErrorCode("Decisions.SummaryTooLong");
    }
}

public sealed class AttachDecisionFileCommandValidator : AbstractValidator<AttachDecisionFileCommand>
{
    public AttachDecisionFileCommandValidator()
    {
        RuleFor(command => command.DecisionId).NotEmpty().WithErrorCode("Decisions.DecisionNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.DocumentFileId).NotEmpty().WithErrorCode("Decisions.DecisionFileInvalid");
    }
}

public sealed class RecordDecisionAppealCommandValidator : AbstractValidator<RecordDecisionAppealCommand>
{
    public RecordDecisionAppealCommandValidator()
    {
        RuleFor(command => command.DecisionId).NotEmpty().WithErrorCode("Decisions.DecisionNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.ReferenceNumber).MaximumLength(DecisionAppeal.MaxReferenceLength)
            .WithErrorCode("Decisions.AppealReferenceTooLong");
        RuleFor(command => command.Notes).MaximumLength(DecisionAppeal.MaxNotesLength)
            .WithErrorCode("Decisions.AppealNotesTooLong");
    }
}

public sealed class RecordAppealOutcomeCommandValidator : AbstractValidator<RecordAppealOutcomeCommand>
{
    public RecordAppealOutcomeCommandValidator()
    {
        RuleFor(command => command.DecisionId).NotEmpty().WithErrorCode("Decisions.DecisionNotFound");
        RuleFor(command => command.AppealId).NotEmpty().WithErrorCode("Decisions.AppealNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.OutcomeId).Must(AppealOutcome.IsRuling)
            .WithErrorCode("Decisions.AppealOutcomeInvalid");
    }
}

public sealed class CancelDecisionCommandValidator : AbstractValidator<CancelDecisionCommand>
{
    public CancelDecisionCommandValidator()
    {
        RuleFor(command => command.DecisionId).NotEmpty().WithErrorCode("Decisions.DecisionNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(DocumentDecision.MaxReasonLength)
            .WithErrorCode("Decisions.CancelReasonRequired");
    }
}

internal static class DecisionTypeRules
{
    public const int MaxName = DecisionType.MaxNameLength;
}
