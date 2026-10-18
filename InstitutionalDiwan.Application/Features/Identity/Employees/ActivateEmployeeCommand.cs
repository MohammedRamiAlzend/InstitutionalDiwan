using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Reactivates a previously deactivated employee record.</summary>
public sealed record ActivateEmployeeCommand(Guid EmployeeId) : ICommand<EmployeeResponse>;
