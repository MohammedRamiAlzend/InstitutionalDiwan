namespace InstitutionalDiwan.Infrastructure.Authentication;

/// <summary>Strongly-typed binding for the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "InstitutionalDiwan";

    public string Audience { get; init; } = "InstitutionalDiwan";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 bytes. Never commit a real production value —
    /// supply it from environment variables, user-secrets, or a secret store. When it is left empty
    /// the server generates one on first start and persists it at <c>Jwt:SigningKeyPath</c>.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 60;
}
