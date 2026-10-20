using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Records the number and date a department wrote in its own register for a document that reached
/// it. The central Diwan number is untouched. A second arrival in the same department reuses the
/// number and only adds a new dated history entry.
/// </summary>
public sealed record RecordDepartmentRegistrationCommand(
    Guid DocumentId,
    Guid DepartmentId,
    string RegistryNumberText,
    DateTime RegistryDate,
    Guid UserId,
    string? Notes = null) : ICommand<DocumentNumberResponse>;
