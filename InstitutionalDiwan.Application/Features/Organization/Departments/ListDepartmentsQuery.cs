using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Lists every department as a flat collection, ordered by code.</summary>
public sealed record ListDepartmentsQuery : IQuery<IReadOnlyCollection<DepartmentResponse>>;
