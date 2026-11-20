using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Builds the OnlyOffice editor config (and a signed token) for a Drive file (§7.2).</summary>
public sealed record GetDriveEditConfigQuery(Guid ItemId) : IQuery<DriveEditConfigResponse>;

internal sealed class GetDriveEditConfigQueryHandler : IQueryHandler<GetDriveEditConfigQuery, DriveEditConfigResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveEditSessionRepository _sessions;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly IDriveOnlyOfficeSettings _settings;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public GetDriveEditConfigQueryHandler(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveEditSessionRepository sessions,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        IDriveOnlyOfficeSettings settings,
        IDriveOnlyOfficeTokenService tokens,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _versions = versions;
        _sessions = sessions;
        _storage = storage;
        _access = access;
        _settings = settings;
        _tokens = tokens;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveEditConfigResponse>> Handle(GetDriveEditConfigQuery request, CancellationToken cancellationToken)
    {
        if (!_settings.IsConfigured || !_tokens.IsConfigured)
            return Result.Failure<DriveEditConfigResponse>(DomainErrors.Drive.OnlyOfficeNotConfigured);

        var resolved = await DriveVersionResolver.ResolveAsync(
            _items, _versions, _access, request.ItemId, null, DriveRole.ViewerId, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<DriveEditConfigResponse>(resolved.Error);
        var (file, version) = resolved.Value;

        var documentType = DriveDocumentTypes.DocumentType(file.Name);
        if (documentType is null)
            return Result.Failure<DriveEditConfigResponse>(DomainErrors.Drive.EditingNotSupported);

        var role = await _access.GetEffectiveRoleAsync(file.ItemId, cancellationToken);
        var canEdit = role >= DriveRole.EditorId && DriveDocumentTypes.IsEditable(file.Name);
        var mode = canEdit ? "edit" : "view";

        var documentKey = $"{file.ItemId:N}-{version.VersionNumber}";
        var session = await EnsureSessionAsync(file.ItemId, version.VersionId, documentKey, cancellationToken);

        Uri documentUrl;
        try
        {
            var lifetime = TimeSpan.FromMinutes(_settings.DocumentUrlLifetimeMinutes <= 0 ? 15 : _settings.DocumentUrlLifetimeMinutes);
            documentUrl = await _storage.CreateDownloadUrlAsync(version.ObjectKey, lifetime, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveEditConfigResponse>(DomainErrors.Drive.StorageNotConfigured);
        }

        var callbackUrl = $"{_settings.CallbackBaseUrl.TrimEnd('/')}/api/drive/onlyoffice/callback";
        var config = new Dictionary<string, object?>
        {
            ["documentType"] = documentType,
            ["document"] = new Dictionary<string, object?>
            {
                ["fileType"] = DriveDocumentTypes.FileType(file.Name),
                ["key"] = documentKey,
                ["title"] = file.Name,
                ["url"] = documentUrl.ToString(),
                ["permissions"] = new Dictionary<string, object?>
                {
                    ["edit"] = canEdit,
                    ["download"] = true,
                    ["print"] = true
                }
            },
            ["editorConfig"] = new Dictionary<string, object?>
            {
                ["callbackUrl"] = callbackUrl,
                ["lang"] = "ar",
                ["mode"] = mode,
                ["user"] = new Dictionary<string, object?>
                {
                    ["id"] = _userContext.UserId.ToString(),
                    ["name"] = string.IsNullOrWhiteSpace(_userContext.Username) ? "مستخدم" : _userContext.Username
                },
                ["customization"] = new Dictionary<string, object?>
                {
                    ["forcesave"] = true,
                    ["autosave"] = true
                }
            }
        };

        config["token"] = _tokens.Sign(config);

        return Result.Success(new DriveEditConfigResponse(
            _settings.ServerUrl, documentType, mode, session.SessionId, version.VersionNumber, config));
    }

    private async Task<DriveEditSession> EnsureSessionAsync(Guid itemId, Guid baseVersionId, string documentKey, CancellationToken cancellationToken)
    {
        // One open session per document key (item + version): collaborators join the same editing
        // room, so reuse the open session regardless of who opened it. The caller's own identity is
        // still placed in the editor config; the session owner is the actor for save callbacks.
        var existing = await _sessions.FindOpenByKeyAsync(documentKey, cancellationToken);
        if (existing is not null)
        {
            existing.Touch(_unitOfWork.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var created = DriveEditSession.Open(itemId, _userContext.UserId, documentKey, baseVersionId, _unitOfWork.UtcNow);
        var session = created.Value;
        _sessions.Add(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }
}