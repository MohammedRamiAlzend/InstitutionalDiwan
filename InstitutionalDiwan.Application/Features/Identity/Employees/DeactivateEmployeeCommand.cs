using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Deactivates an employee record (soft delete).</summary>
public sealed record DeactivateEmployeeCommand(Guid EmployeeId) : ICommand<EmployeeResponse>;
