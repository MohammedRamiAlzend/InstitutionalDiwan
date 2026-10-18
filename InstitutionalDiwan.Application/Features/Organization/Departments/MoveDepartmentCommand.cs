using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>
/// Reparents a department. Pass <c>null</c> to promote it to a root department. The move is rejected
/// when the new parent is the department itself, one of its own descendants (which would create a
/// cycle), missing, or inactive.
/// </summary>
public sealed record MoveDepartmentCommand(
    Guid DepartmentId,
    Guid? NewParentDepartmentId) : ICommand<DepartmentResponse>;
