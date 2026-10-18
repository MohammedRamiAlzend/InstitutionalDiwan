using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Lists every employee record (admin-facing).</summary>
public sealed record ListEmployeesQuery : IQuery<IReadOnlyCollection<EmployeeResponse>>;
