using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Lists all user accounts with their roles (admin-facing).</summary>
public sealed record ListUsersQuery : IQuery<IReadOnlyCollection<UserResponse>>;
