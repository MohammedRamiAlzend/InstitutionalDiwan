using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.DepartmentRecords;

/// <summary>
/// Replaces the record values of an open visit. Only the department that keeps the record may edit it,
/// and only while the document is still there; a blank value clears that field.
/// </summary>
public sealed record SetVisitRecordCommand(
    Guid VisitId,
    Guid UserId,
    IReadOnlyList<DocumentFieldValueInput> Values) : ICommand<DepartmentRecordVisitResponse>;

/// <summary>Corrects the record values of a locked visit, with a mandatory reason (administrators).</summary>
public sealed record CorrectVisitRecordCommand(
    Guid VisitId,
    Guid UserId,
    IReadOnlyList<DocumentFieldValueInput> Values,
    string Reason) : ICommand<DepartmentRecordVisitResponse>;

/// <summary>The department records on a document, filtered to the ones the caller may read.</summary>
public sealed record GetDocumentDepartmentRecordsQuery(Guid DocumentId) : IQuery<DocumentDepartmentRecordsResponse>;

public sealed class SetVisitRecordCommandValidator : AbstractValidator<SetVisitRecordCommand>
{
    public SetVisitRecordCommandValidator()
    {
        RuleFor(command => command.VisitId).NotEmpty().WithErrorCode("DepartmentRecords.VisitNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.Values).NotNull();
    }
}

public sealed class CorrectVisitRecordCommandValidator : AbstractValidator<CorrectVisitRecordCommand>
{
    public CorrectVisitRecordCommandValidator()
    {
        RuleFor(command => command.VisitId).NotEmpty().WithErrorCode("DepartmentRecords.VisitNotFound");
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode("Identity.UserIdEmpty");
        RuleFor(command => command.Values).NotNull();
        RuleFor(command => command.Reason)
            .NotEmpty().WithErrorCode("DepartmentRecords.CorrectionReasonRequired")
            .MaximumLength(DocumentRegistryVisit.MaxCorrectionReasonLength).WithErrorCode("DepartmentRecords.CorrectionReasonTooLong");
    }
}
