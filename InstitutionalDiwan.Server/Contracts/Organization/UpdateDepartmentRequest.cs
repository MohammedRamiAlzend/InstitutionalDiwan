namespace InstitutionalDiwan.Server.Contracts.Organization;

/// <summary>
/// Payload submitted to <c>PUT /api/departments/{departmentId}</c>. <c>ParentDepartmentId</c> of
/// <c>null</c> promotes the department to a root and <c>ManagerEmployeeId</c> of <c>null</c> clears
/// the manager; the code and department type are immutable.
/// </summary>
public sealed record UpdateDepartmentRequest(
    string NameAr,
    Guid? ParentDepartmentId,
    bool CanSendExternal = false,
    bool CanReceiveExternal = false,
    string? NameEn = null,
    Guid? ManagerEmployeeId = null,
    string? Notes = null);
