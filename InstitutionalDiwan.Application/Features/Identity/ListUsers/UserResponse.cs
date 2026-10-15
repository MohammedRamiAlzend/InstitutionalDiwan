namespace InstitutionalDiwan.Application.Features.Identity.ListUsers;

/// <summary>Summary of a user account for admin listings.</summary>
public sealed record UserResponse(
    Guid UserId,
    string Username,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    IReadOnlyCollection<string> Roles);
