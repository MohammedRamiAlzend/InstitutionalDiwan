namespace InstitutionalDiwan.Application.Features.Identity.Login;

/// <summary>
/// Identity of the authenticated caller: the user id and username, the role codes and permission
/// codes the access token grants, and the departments the caller's live scope reaches
/// (<c>null</c> for an institution-wide scope, which reaches every department). Route guards and UI
/// gating read this so what the client believes about the caller matches what the API enforces.
/// </summary>
public sealed record UserProfileResponse(
    Guid UserId,
    string Username,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<Guid>? AccessibleDepartmentIds);
