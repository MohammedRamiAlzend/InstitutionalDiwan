using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Fetches a single role, including its granted permission codes, by id.</summary>
public sealed record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleResponse>;
