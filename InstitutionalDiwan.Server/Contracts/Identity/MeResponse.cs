namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Identity of the authenticated caller returned by <c>GET /api/auth/me</c>.</summary>
public sealed record MeResponse(
    Guid UserId,
    string Username,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
