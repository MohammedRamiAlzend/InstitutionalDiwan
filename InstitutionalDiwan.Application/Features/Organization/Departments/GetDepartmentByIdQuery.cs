using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Fetches a single department by its id.</summary>
public sealed record GetDepartmentByIdQuery(Guid DepartmentId) : IQuery<DepartmentResponse>;
