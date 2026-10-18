namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>PUT /api/roles/{roleId}</c>. The role code is immutable.</summary>
public sealed record UpdateRoleRequest(
    string NameAr,
    string? NameEn = null,
    string? Description = null);
