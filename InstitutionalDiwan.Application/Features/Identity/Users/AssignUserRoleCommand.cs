using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Assigns an existing role to a user, recording who granted it.</summary>
public sealed record AssignUserRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid? AssignedByUserId = null) : ICommand<UserResponse>;
