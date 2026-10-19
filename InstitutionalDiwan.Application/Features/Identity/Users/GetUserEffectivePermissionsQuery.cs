using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Resolves what a user can actually do right now: the permissions and roles their role grants
/// confer, their organizational scope type, primary department, and the departments reachable
/// through active timed assignments.
/// </summary>
public sealed record GetUserEffectivePermissionsQuery(Guid UserId)
    : IQuery<UserEffectivePermissionsResponse>;
