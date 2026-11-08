using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Receives the movement's current step at its destination department and writes the dated visit in
/// that department's register. When the department already has a number for the document in the
/// year of <see cref="RegistryDate"/> (today when omitted) that number is reused; otherwise
/// <see cref="RegistryNumberText"/> is required, so custody can never be taken without a register entry.
/// </summary>
public sealed record ReceiveDocumentCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null,
    Guid? StepId = null,
    DateTime? RegistryDate = null,
    string? RegistryNumberText = null,
    Guid? RegistryId = null) : ICommand<MovementStepReceiptResponse>;

/// <summary>What a completed receipt recorded.</summary>
public sealed record MovementStepReceiptResponse(
    Guid MovementId,
    int StepNo,
    Guid DepartmentId,
    Guid RegistryNumberId,
    string RegistryNumberText,
    DateTime? RegistryDate,
    DateTime ReceivedAt,
    Guid RegistryVisitId,
    bool IsReturnVisit);
