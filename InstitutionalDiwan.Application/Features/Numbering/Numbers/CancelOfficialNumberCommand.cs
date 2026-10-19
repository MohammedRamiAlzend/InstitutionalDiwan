using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Cancels an active official number with a required reason. A cancelled sequence value is never
/// reused, so the registry stays gap-free in the audit sense.
/// </summary>
public sealed record CancelOfficialNumberCommand(
    Guid DocumentId,
    Guid DocumentNumberId,
    Guid UserId,
    string Reason) : ICommand<DocumentNumberResponse>;
