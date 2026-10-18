namespace InstitutionalDiwan.Server.Contracts.Organization;

/// <summary>Payload submitted to <c>POST /api/departments</c>.</summary>
public sealed record CreateDepartmentRequest(
    string Code,
    string NameAr,
    string DepartmentTypeCode,
    Guid? ParentDepartmentId = null,
    string? NameEn = null,
    bool CanSendExternal = false,
    bool CanReceiveExternal = false,
    string? Notes = null);
