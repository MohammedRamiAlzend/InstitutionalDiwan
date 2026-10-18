namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/roles</c>.</summary>
public sealed record CreateRoleRequest(
    string Code,
    string NameAr,
    string? NameEn = null,
    string? Description = null);
