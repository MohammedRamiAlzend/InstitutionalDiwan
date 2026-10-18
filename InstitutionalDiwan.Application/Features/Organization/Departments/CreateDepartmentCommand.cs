using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Creates a new department under an optional parent department.</summary>
public sealed record CreateDepartmentCommand(
    string Code,
    string NameAr,
    int DepartmentTypeId,
    Guid? ParentDepartmentId = null,
    string? NameEn = null,
    bool CanSendExternal = false,
    bool CanReceiveExternal = false,
    string? Notes = null) : ICommand<DepartmentResponse>;
