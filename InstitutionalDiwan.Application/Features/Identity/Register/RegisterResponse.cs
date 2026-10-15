namespace InstitutionalDiwan.Application.Features.Identity.Register;

/// <summary>Result of a successful registration: the new account's identity and assigned roles.</summary>
public sealed record RegisterResponse(
    Guid UserId,
    string Username,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles);
