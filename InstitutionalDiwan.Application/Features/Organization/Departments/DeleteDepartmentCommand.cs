using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>
/// Soft-deletes a department by deactivating it. Departments that still have active children are
/// rejected with <c>Department.HasActiveChildren</c> (409).
/// </summary>
public sealed record DeleteDepartmentCommand(Guid DepartmentId) : ICommand;
