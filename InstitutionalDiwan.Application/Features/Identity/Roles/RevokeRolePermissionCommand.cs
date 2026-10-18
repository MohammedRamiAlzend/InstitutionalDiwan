using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Revokes a permission from a role. Revoking a permission the role does not hold is a no-op.</summary>
public sealed record RevokeRolePermissionCommand(Guid RoleId, string PermissionCode) : ICommand<RoleResponse>;
