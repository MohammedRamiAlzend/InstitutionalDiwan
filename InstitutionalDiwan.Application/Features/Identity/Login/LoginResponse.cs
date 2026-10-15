namespace InstitutionalDiwan.Application.Features.Identity.Login;

/// <summary>Result of a successful authentication: the signed access token plus identity context.</summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Username,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
