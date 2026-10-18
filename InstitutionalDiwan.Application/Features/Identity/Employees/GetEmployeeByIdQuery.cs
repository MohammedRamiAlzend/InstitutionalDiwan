using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Fetches a single employee by its id.</summary>
public sealed record GetEmployeeByIdQuery(Guid EmployeeId) : IQuery<EmployeeResponse>;
