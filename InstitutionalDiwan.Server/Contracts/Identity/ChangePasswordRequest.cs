namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/auth/change-password</c>.</summary>
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
