namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/auth/register</c>.</summary>
public sealed record RegisterRequest(string Username, string Password);
