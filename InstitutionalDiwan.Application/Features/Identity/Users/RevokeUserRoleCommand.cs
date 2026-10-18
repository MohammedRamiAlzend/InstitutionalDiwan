using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Revokes a role previously assigned to a user.</summary>
public sealed record RevokeUserRoleCommand(Guid UserId, Guid RoleId) : ICommand<UserResponse>;
