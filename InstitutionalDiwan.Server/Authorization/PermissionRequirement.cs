using Microsoft.AspNetCore.Authorization;

namespace InstitutionalDiwan.Server.Authorization;

/// <summary>Requires the authenticated caller's JWT to carry the given permission claim.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    /// <summary>The permission code the token must contain (policy name matches this value).</summary>
    public string Permission { get; }
}
