using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Reports whether OnlyOffice editing and Drive object storage are configured (Module 11). Returns
/// presence flags and the non-secret URLs only, so it is safe to expose to any Drive user.
/// </summary>
public sealed record GetDriveReadinessQuery : IQuery<DriveReadinessResponse>;

/// <summary>
/// Readiness of the Module 11 editing path. <see cref="IsReady"/> is true only when OnlyOffice and
/// storage are configured; no secret value appears anywhere in this payload.
/// </summary>
public sealed record DriveReadinessResponse(
    bool IsReady,
    DriveOnlyOfficeReadiness OnlyOffice,
    DriveStorageReadiness Storage);

/// <summary>
/// OnlyOffice readiness: the enable switch, which non-secret inputs are present, the JWT secret
/// presence as a boolean, and the resolved save-callback URL.
/// </summary>
public sealed record DriveOnlyOfficeReadiness(
    bool Enabled,
    bool ServerUrlConfigured,
    bool CallbackBaseUrlConfigured,
    bool JwtSecretConfigured,
    bool IsConfigured,
    string? ServerUrl,
    string? CallbackBaseUrl,
    string? CallbackUrl);

/// <summary>Storage readiness: whether the endpoint, bucket and credentials are all present.</summary>
public sealed record DriveStorageReadiness(bool IsConfigured);

internal sealed class GetDriveReadinessQueryHandler
    : IQueryHandler<GetDriveReadinessQuery, DriveReadinessResponse>
{
    private readonly IDriveOnlyOfficeSettings _settings;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly IDriveStorageService _storage;

    public GetDriveReadinessQueryHandler(
        IDriveOnlyOfficeSettings settings,
        IDriveOnlyOfficeTokenService tokens,
        IDriveStorageService storage)
    {
        _settings = settings;
        _tokens = tokens;
        _storage = storage;
    }

    public Task<Result<DriveReadinessResponse>> Handle(
        GetDriveReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var serverUrl = NullIfBlank(_settings.ServerUrl);
        var callbackBaseUrl = NullIfBlank(_settings.CallbackBaseUrl);
        var callbackUrl = callbackBaseUrl is null
            ? null
            : $"{callbackBaseUrl.TrimEnd('/')}/api/drive/onlyoffice/callback";

        var onlyOffice = new DriveOnlyOfficeReadiness(
            _settings.Enabled,
            serverUrl is not null,
            callbackBaseUrl is not null,
            _tokens.IsConfigured,
            _settings.IsConfigured,
            serverUrl,
            callbackBaseUrl,
            callbackUrl);

        var storage = new DriveStorageReadiness(_storage.IsConfigured);
        var isReady = onlyOffice.IsConfigured && storage.IsConfigured;

        return Task.FromResult(Result.Success(
            new DriveReadinessResponse(isReady, onlyOffice, storage)));
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
