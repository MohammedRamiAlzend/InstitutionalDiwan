namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/roles/{roleId}/permissions</c>.</summary>
public sealed record GrantPermissionRequest(string PermissionCode);
