namespace InstitutionalDiwan.Infrastructure.OnlyOffice;

using InstitutionalDiwan.Application.Abstractions.Drive;

/// <summary>
/// Connection settings for the self-hosted OnlyOffice Document Server (Module 11 editing). The
/// server URL and callback base URL are non-secret; the JWT secret belongs in user-secrets or
/// environment variables, never in committed appsettings. The callback base URL must be reachable
/// from the OnlyOffice server, and the SeaweedFS presigned host must be reachable from both the
/// browser and the OnlyOffice server.
/// </summary>
public sealed class DriveOnlyOfficeOptions : IDriveOnlyOfficeSettings
{
    public const string SectionName = "Drive:OnlyOffice";

    /// <summary>Public URL of the OnlyOffice Document Server the browser loads the editor API from.</summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>HS256 shared secret used to sign the editor config and verify the save callback.</summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>Public base URL of this API, used to build the save callback URL OnlyOffice posts to.</summary>
    public string CallbackBaseUrl { get; set; } = string.Empty;

    /// <summary>Lifetime of the presigned document URL handed to OnlyOffice (minutes).</summary>
    public int DocumentUrlLifetimeMinutes { get; set; } = 15;

    /// <summary>Lifetime of the signed editor-config / callback token (minutes).</summary>
    public int TokenLifetimeMinutes { get; set; } = 60;

    public bool Enabled { get; set; } = true;

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(ServerUrl) &&
        !string.IsNullOrWhiteSpace(JwtSecret) &&
        !string.IsNullOrWhiteSpace(CallbackBaseUrl);
}