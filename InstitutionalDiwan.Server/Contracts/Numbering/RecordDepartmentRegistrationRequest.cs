namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>
/// Payload submitted to <c>POST /api/documents/{documentId}/numbers/department-registration</c>.
/// The number and date are typed by the department clerk exactly as written in that department's
/// register; nothing is allocated. Submitting the same department again keeps the number and only
/// records the new date. The department must be the caller's own; <c>RegistryId</c> optionally
/// chooses which of the department's registers the number belongs to.
/// </summary>
public sealed record RecordDepartmentRegistrationRequest(
    Guid DepartmentId,
    string RegistryNumberText,
    DateTime RegistryDate,
    string? Notes = null,
    Guid? RegistryId = null);
