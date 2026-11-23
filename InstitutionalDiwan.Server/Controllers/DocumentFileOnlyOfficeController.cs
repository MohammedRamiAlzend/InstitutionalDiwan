using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Application.Features.Attachments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Server.Contracts.Attachments;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/document-files")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class DocumentFileOnlyOfficeController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly IDocumentFileRepository _files;
    private readonly IFileStorageProviderRepository _providers;
    private readonly IFileStorageService _storage;

    public DocumentFileOnlyOfficeController(
        ISender sender,
        IDriveOnlyOfficeTokenService tokens,
        IDocumentFileRepository files,
        IFileStorageProviderRepository providers,
        IFileStorageService storage)
    {
        _sender = sender;
        _tokens = tokens;
        _files = files;
        _providers = providers;
        _storage = storage;
    }

    [HttpGet("{fileId:guid}/edit-config")]
    [EndpointSummary("Builds the OnlyOffice editor configuration for a document attachment.")]
    [EndpointDescription("Returns a signed OnlyOffice configuration for a Word or Excel attachment. Managers receive edit mode and other viewers receive read-only mode. Responses: 200 (The editor configuration.), 400 (The file type is unsupported.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the document.), 404 (The file or version does not exist.), 500 (OnlyOffice is not configured.)")]
    [ProducesResponseType<AttachmentEditConfigResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EditConfig(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetAttachmentEditConfigQuery(fileId), cancellationToken)).ToActionResult();

    [HttpGet("{fileId:guid}/onlyoffice-content")]
    [AllowAnonymous]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> Content(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        [FromQuery] Guid versionId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var claims = _tokens.Verify(token);
        if (claims is null ||
            !string.Equals(GetClaim(claims, "kind"), "document-attachment", StringComparison.Ordinal) ||
            !Guid.TryParse(GetClaim(claims, "fileId"), out var claimedFileId) ||
            claimedFileId != fileId ||
            !Guid.TryParse(GetClaim(claims, "versionId"), out var claimedVersionId) ||
            claimedVersionId != versionId)
            return Unauthorized();

        var file = await _files.GetByIdAsync(fileId, cancellationToken);
        var version = file?.Versions.FirstOrDefault(item => item.VersionId == versionId);
        if (file is null || version is null)
            return NotFound();

        var provider = await _providers.GetByIdAsync(version.StorageProviderId, cancellationToken);
        if (provider is null)
            return NotFound();

        var stream = await _storage.OpenReadAsync(provider.BasePathOrEndpoint, version.RelativePath, cancellationToken);
        return stream is null ? NotFound() : File(stream, version.MimeType, enableRangeProcessing: true);
    }

    [HttpPost("onlyoffice-callback")]
    [AllowAnonymous]
    [EndpointSummary("Receives an OnlyOffice save callback for a document attachment.")]
    [EndpointDescription("Validates the signed callback, downloads the edited bytes, rejects stale or duplicate saves, and appends a new immutable document-file version. The HTTP response is always 200 with error=0 or error=1 for the OnlyOffice protocol.")]
    [ProducesResponseType<AttachmentOnlyOfficeCallbackResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback(
        [FromBody] AttachmentOnlyOfficeCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new AttachmentOnlyOfficeCallbackPayload(request.Key, request.Status, request.Url, request.Token);
        var result = await _sender.Send(new ProcessAttachmentOnlyOfficeCallbackCommand(payload), cancellationToken);
        return Ok(result.IsSuccess ? result.Value : AttachmentOnlyOfficeCallbackResult.Failed);
    }

    private static string? GetClaim(IReadOnlyDictionary<string, object?> claims, string key) =>
        claims.TryGetValue(key, out var value) ? value?.ToString() : null;
}
