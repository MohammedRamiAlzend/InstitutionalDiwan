using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Records the number and date a department wrote in its own register for a document that reached
/// it. The central Diwan number is untouched. A second arrival in the same department reuses the
/// number and only adds a new dated history entry. <see cref="DepartmentId"/> must be the caller's
/// own department; <see cref="RegistryId"/> picks one of that department's registers (the best
/// matching active register is used when omitted).
/// </summary>
public sealed record RecordDepartmentRegistrationCommand(
    Guid DocumentId,
    Guid DepartmentId,
    string RegistryNumberText,
    DateTime RegistryDate,
    Guid UserId,
    string? Notes = null,
    Guid? RegistryId = null) : ICommand<DocumentNumberResponse>;
