namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/users/{userId}/reset-password</c>.</summary>
public sealed record ResetUserPasswordRequest(string NewPassword);
