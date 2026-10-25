namespace InstitutionalDiwan.Application.Features.Identity.Login;

/// <summary>
/// Identity of the authenticated caller as carried by the access token: the user id and username,
/// plus the role codes and permission codes the token grants. Route guards and UI gating read this
/// so what the client believes about the caller matches what the API enforces.
/// </summary>
public sealed record UserProfileResponse(
    Guid UserId,
    string Username,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
