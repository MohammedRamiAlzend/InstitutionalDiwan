namespace InstitutionalDiwan.Application.Abstractions.Drive;

/// <summary>
/// The OnlyOffice connection settings the Drive editing use cases need (Module 11), kept in the
/// Application layer so handlers stay host-agnostic. The concrete values (and the JWT secret) are
/// bound from <c>Drive:OnlyOffice</c> configuration in Infrastructure.
/// </summary>
public interface IDriveOnlyOfficeSettings
{
    /// <summary>True when editing is enabled and the server URL, callback base URL and secret are set.</summary>
    bool IsConfigured { get; }

    /// <summary>Public URL of the OnlyOffice Document Server the browser loads the editor API from.</summary>
    string ServerUrl { get; }

    /// <summary>Public base URL of this API, used to build the save callback URL.</summary>
    string CallbackBaseUrl { get; }

    /// <summary>Lifetime of the presigned document URL handed to OnlyOffice (minutes).</summary>
    int DocumentUrlLifetimeMinutes { get; }
}