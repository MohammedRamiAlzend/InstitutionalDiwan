namespace InstitutionalDiwan.Application.Abstractions.Drive;

/// <summary>
/// Signs and verifies the HS256 JWTs exchanged with the OnlyOffice Document Server (Module 11).
/// The editor config is signed so OnlyOffice trusts it; the save callback carries a token signed
/// with the same shared secret. The payload is an arbitrary JSON object (nested structures intact),
/// not a flat claim set. The secret comes from configuration, never from callers.
/// </summary>
public interface IDriveOnlyOfficeTokenService
{
    /// <summary>True when a signing secret is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Signs the payload as a JWT whose body is exactly the payload JSON.</summary>
    string Sign(IReadOnlyDictionary<string, object?> payload);

    /// <summary>Verifies the signature and expiry; returns the payload, or null when invalid.</summary>
    IReadOnlyDictionary<string, object?>? Verify(string? token);
}