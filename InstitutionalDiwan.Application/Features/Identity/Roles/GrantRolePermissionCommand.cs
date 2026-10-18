using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Grants a permission from the catalog to a role.</summary>
public sealed record GrantRolePermissionCommand(Guid RoleId, string PermissionCode) : ICommand<RoleResponse>;
