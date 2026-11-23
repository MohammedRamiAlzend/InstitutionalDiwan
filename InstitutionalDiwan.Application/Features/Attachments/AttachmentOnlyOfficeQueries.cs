using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed record GetAttachmentEditConfigQuery(Guid FileId) : IQuery<AttachmentEditConfigResponse>;

internal sealed class GetAttachmentEditConfigQueryHandler
    : IQueryHandler<GetAttachmentEditConfigQuery, AttachmentEditConfigResponse>
{
    private readonly IDocumentFileRepository _files;
    private readonly IFileStorageProviderRepository _providers;
    private readonly IFileStorageService _storage;
    private readonly IDocumentAccessEvaluator _access;
    private readonly IDriveOnlyOfficeSettings _settings;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly IUserContext _userContext;

    public GetAttachmentEditConfigQueryHandler(
        IDocumentFileRepository files,
        IFileStorageProviderRepository providers,
        IFileStorageService storage,
        IDocumentAccessEvaluator access,
        IDriveOnlyOfficeSettings settings,
        IDriveOnlyOfficeTokenService tokens,
        IUserContext userContext)
    {
        _files = files;
        _providers = providers;
        _storage = storage;
        _access = access;
        _settings = settings;
        _tokens = tokens;
        _userContext = userContext;
    }

    public async Task<Result<AttachmentEditConfigResponse>> Handle(
        GetAttachmentEditConfigQuery request,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsConfigured || !_tokens.IsConfigured)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Drive.OnlyOfficeNotConfigured);

        var file = await _files.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Attachments.FileNotFound);

        if (!await _access.CanAccessDocumentAsync(file.DocumentId, AccessLevel.ViewId, cancellationToken))
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Correspondence.DocumentAccessDenied);

        var version = file.Versions.FirstOrDefault(item => item.IsCurrent);
        if (version is null)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Attachments.VersionNotFound);

        var documentType = AttachmentDocumentTypes.DocumentType(version.OriginalFileName);
        if (documentType is null)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Drive.EditingNotSupported);

        var provider = await _providers.GetByIdAsync(version.StorageProviderId, cancellationToken);
        if (provider is null)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Attachments.StorageProviderNotFound);

        var content = await _storage.OpenReadAsync(
            provider.BasePathOrEndpoint, version.RelativePath, cancellationToken);
        if (content is null)
            return Result.Failure<AttachmentEditConfigResponse>(DomainErrors.Attachments.FileNotFound);
        await content.DisposeAsync();

        var key = $"{file.FileId:N}-{version.VersionNumber}";
        var tokenPayload = new Dictionary<string, object?>
        {
            ["kind"] = "document-attachment",
            ["fileId"] = file.FileId.ToString("D"),
            ["versionId"] = version.VersionId.ToString("D"),
            ["userId"] = _userContext.UserId.ToString("D"),
            ["key"] = key
        };
        var token = _tokens.Sign(tokenPayload);
        var baseUrl = _settings.CallbackBaseUrl.TrimEnd('/');
        var contentUrl = $"{baseUrl}/api/document-files/{file.FileId}/onlyoffice-content?versionId={version.VersionId}&token={Uri.EscapeDataString(token)}";
        var callbackUrl = $"{baseUrl}/api/document-files/onlyoffice-callback";
        var canEdit = await _access.CanAccessDocumentAsync(file.DocumentId, AccessLevel.ManageId, cancellationToken);
        var mode = canEdit && AttachmentDocumentTypes.IsEditable(version.OriginalFileName) ? "edit" : "view";

        var config = new Dictionary<string, object?>
        {
            ["documentType"] = documentType,
            ["document"] = new Dictionary<string, object?>
            {
                ["fileType"] = AttachmentDocumentTypes.FileType(version.OriginalFileName),
                ["key"] = key,
                ["title"] = version.OriginalFileName,
                ["url"] = contentUrl,
                ["permissions"] = new Dictionary<string, object?>
                {
                    ["edit"] = mode == "edit",
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
                    ["id"] = _userContext.UserId.ToString("D"),
                    ["name"] = string.IsNullOrWhiteSpace(_userContext.Username) ? "مستخدم" : _userContext.Username
                },
                ["customization"] = new Dictionary<string, object?>
                {
                    ["forcesave"] = true,
                    ["autosave"] = true
                }
            }
        };
        config["token"] = token;

        return Result.Success(new AttachmentEditConfigResponse(
            _settings.ServerUrl, documentType, mode, version.VersionNumber, config));
    }
}
