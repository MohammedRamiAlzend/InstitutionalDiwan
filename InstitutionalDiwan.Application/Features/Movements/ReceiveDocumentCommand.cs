using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Receives the movement's current step at its destination department. Fails unless that department
/// already recorded its own register number for the document, so custody can never be taken without
/// a register entry.
/// </summary>
public sealed record ReceiveDocumentCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null,
    Guid? StepId = null) : ICommand<MovementStepReceiptResponse>;

/// <summary>What a completed receipt recorded.</summary>
public sealed record MovementStepReceiptResponse(
    Guid MovementId,
    int StepNo,
    Guid DepartmentId,
    Guid RegistryNumberId,
    string RegistryNumberText,
    DateTime? RegistryDate,
    DateTime ReceivedAt);
