using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.ListUsers;

/// <summary>Lists all user accounts with their roles (admin-facing).</summary>
public sealed record ListUsersQuery : IQuery<IReadOnlyCollection<UserResponse>>;
