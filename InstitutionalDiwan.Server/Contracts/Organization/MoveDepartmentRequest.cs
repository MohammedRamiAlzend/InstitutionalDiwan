namespace InstitutionalDiwan.Server.Contracts.Organization;

/// <summary>
/// Payload submitted to <c>PUT /api/departments/{departmentId}/parent</c>. A <c>null</c>
/// <c>ParentDepartmentId</c> promotes the department to a root.
/// </summary>
public sealed record MoveDepartmentRequest(Guid? ParentDepartmentId);
