namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Credentials submitted to <c>POST /api/auth/login</c>.</summary>
public sealed record LoginRequest(string Username, string Password);
