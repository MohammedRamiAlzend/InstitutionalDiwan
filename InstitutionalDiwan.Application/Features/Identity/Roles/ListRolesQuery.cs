using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Lists every role with its granted permission codes.</summary>
public sealed record ListRolesQuery : IQuery<IReadOnlyCollection<RoleResponse>>;
