using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>
/// Updates the editable attributes of an existing department: its Arabic/English name, its parent
/// (pass <c>null</c> to promote it to a root department) and its external-correspondence policy.
/// The code and department type are immutable once created.
/// </summary>
public sealed record UpdateDepartmentCommand(
    Guid DepartmentId,
    string NameAr,
    Guid? ParentDepartmentId,
    bool CanSendExternal,
    bool CanReceiveExternal,
    string? NameEn = null) : ICommand<DepartmentResponse>;
