using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Flat read model returned by the department endpoints.</summary>
public sealed record DepartmentResponse(
    Guid DepartmentId,
    string Code,
    string NameAr,
    string? NameEn,
    string DepartmentTypeCode,
    Guid? ParentDepartmentId,
    Guid? ManagerEmployeeId,
    bool IsActive,
    bool CanSendExternal,
    bool CanReceiveExternal,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static DepartmentResponse From(Department department) => new(
        department.DepartmentId,
        department.Code,
        department.NameAr,
        department.NameEn,
        department.DepartmentTypeCode,
        department.ParentDepartmentId,
        department.ManagerEmployeeId,
        department.IsActive,
        department.CanSendExternal,
        department.CanReceiveExternal,
        department.Notes,
        department.CreatedAt,
        department.UpdatedAt);
}
