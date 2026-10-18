using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Reactivates a previously deactivated department.</summary>
public sealed record ActivateDepartmentCommand(Guid DepartmentId) : ICommand<DepartmentResponse>;
