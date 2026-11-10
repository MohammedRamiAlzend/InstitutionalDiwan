using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Receives the movement's current step at its destination department and writes the dated visit in
/// that department's register. When the department already has a number for the document in the
/// year of <see cref="RegistryDate"/> (today when omitted) that number is reused; otherwise
/// <see cref="RegistryNumberText"/> is required, so custody can never be taken without a register entry.
/// When the department has a published record form for the document type, <see cref="FieldValues"/>
/// opens the department's record on the visit in the same transaction; every required field must be
/// filled or the whole receipt fails.
/// With <see cref="IssueNewNumber"/> a department that already has a number for the document in that
/// year writes it under <see cref="RegistryNumberText"/> instead: the earlier number becomes
/// <c>REPLACED</c> and stays linked to the new one, which is the number in use from then on.
/// </summary>
public sealed record ReceiveDocumentCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null,
    Guid? StepId = null,
    DateTime? RegistryDate = null,
    string? RegistryNumberText = null,
    Guid? RegistryId = null,
    IReadOnlyList<DocumentFieldValueInput>? FieldValues = null,
    bool IssueNewNumber = false) : ICommand<MovementStepReceiptResponse>;

/// <summary>
/// What a completed receipt recorded. <see cref="ReplacedNumberId"/> and <see cref="ReplacedNumberText"/>
/// name the department's earlier number when this receipt wrote the document under a new one.
/// </summary>
public sealed record MovementStepReceiptResponse(
    Guid MovementId,
    int StepNo,
    Guid DepartmentId,
    Guid RegistryNumberId,
    string RegistryNumberText,
    DateTime? RegistryDate,
    DateTime ReceivedAt,
    Guid RegistryVisitId,
    bool IsReturnVisit,
    Guid? ReplacedNumberId = null,
    string? ReplacedNumberText = null);
