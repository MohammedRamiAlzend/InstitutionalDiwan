using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Returns the full department hierarchy as a collection of root nodes.</summary>
public sealed record GetDepartmentTreeQuery : IQuery<IReadOnlyCollection<DepartmentTreeNodeResponse>>;
