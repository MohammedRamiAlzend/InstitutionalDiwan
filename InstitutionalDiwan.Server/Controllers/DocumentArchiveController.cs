using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Archiving;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Module 07: moves documents into and out of the archive and searches what is archived.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class DocumentArchiveController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentArchiveController(ISender sender) => _sender = sender;

    /// <summary>Lists a page of the archived documents the caller may view.</summary>
    /// <remarks>
    /// Most recently archived first by default. A caller sees documents they created, documents they hold
    /// a valid access grant on, and documents of departments in their scope unless those are SECRET or
    /// TOP_SECRET; the paging counters follow the same rule. Requires the <c>DOCUMENT_VIEW</c> permission.
    /// </remarks>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Free-text search across title, summary, and official number (string, optional). Example: <c>2026</c>.</param>
    /// <param name="sortBy">Sort key: <c>archivedAt</c>, <c>title</c>, or <c>officialNumber</c> (string, optional). Example: <c>title</c>.</param>
    /// <param name="sortDescending">Sort descending when true (boolean, optional). Example: <c>true</c>.</param>
    /// <param name="owningDepartmentId">Filter by owning department id (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="documentTypeId">Filter by document type id (integer, optional). Example: <c>1</c>.</param>
    /// <param name="directionId">Filter by direction id (integer, optional). Example: <c>1</c>.</param>
    /// <param name="archiveLocationId">Filter by archive location id (integer, optional). Example: <c>2</c>.</param>
    /// <param name="archivedFromUtc">Archived at or after this UTC time (date-time, optional). Example: <c>2026-01-01T00:00:00Z</c>.</param>
    /// <param name="archivedToUtc">Archived at or before this UTC time (date-time, optional). Example: <c>2026-12-31T23:59:59Z</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of archived documents with the paging counters.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet("archive/documents")]
    [EndpointSummary("Lists a page of the archived documents the caller may view.")]
    [EndpointDescription("Lists a page of the archived documents the caller may view, most recently archived first by default. A caller sees documents they created, documents they hold a valid access grant on, and documents of departments in their scope unless those are SECRET or TOP_SECRET; the paging counters follow the same rule. Requires the DOCUMENT_VIEW permission. Responses: 200 (One page of archived documents with the paging counters.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [ProducesResponseType<PagedResult<ArchivedDocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Free-text search across title, summary, and official number (string, optional). Example: 2026.")] [FromQuery] string? search = null,
        [Description("Sort key: archivedAt, title, officialNumber (string, optional). Example: title.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        [Description("Filter by owning department id (GUID, optional). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] [FromQuery] Guid? owningDepartmentId = null,
        [Description("Filter by document type id (integer, optional). Example: 1.")] [FromQuery] int? documentTypeId = null,
        [Description("Filter by direction id (integer, optional). Example: 1.")] [FromQuery] int? directionId = null,
        [Description("Filter by archive location id (integer, optional). Example: 2.")] [FromQuery] int? archiveLocationId = null,
        [Description("Archived at or after this UTC time (date-time, optional). Example: 2026-01-01T00:00:00Z.")] [FromQuery] DateTime? archivedFromUtc = null,
        [Description("Archived at or before this UTC time (date-time, optional). Example: 2026-12-31T23:59:59Z.")] [FromQuery] DateTime? archivedToUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SearchArchivedDocumentsQuery(
                page,
                pageSize,
                search,
                sortBy,
                sortDescending,
                owningDepartmentId,
                documentTypeId,
                directionId,
                archiveLocationId,
                archivedFromUtc,
                archivedToUtc),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Returns a document's archive history, newest first.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The archive events.</response>
    /// <response code="403">The caller lacks DOCUMENT_VIEW or VIEW access on this document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("documents/{documentId:guid}/archive-events")]
    [EndpointSummary("Returns a document's archive history, newest first.")]
    [EndpointDescription("Returns a document's archive history, newest first. Requires the DOCUMENT_VIEW permission and VIEW access on the document. Responses: 200 (The archive events.), 403 (The caller lacks DOCUMENT_VIEW or VIEW access on this document.), 404 (No document with that id exists.)")]
    [ProducesResponseType<IReadOnlyCollection<DocumentArchiveEventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEvents(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentArchiveEventsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Archives a registered document at an archive location.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>archiveLocationId</term><description>Active archive location id (integer, required). Example: <c>2</c>.</description></item>
    /// <item><term>reason</term><description>Reason, at most 500 characters (string, optional). Example: <c>Correspondence closed.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The archived document.</response>
    /// <response code="400">The location is unknown or inactive, or the reason is too long.</response>
    /// <response code="403">The caller lacks DOCUMENT_ARCHIVE or MANAGE access on this document.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not registered, is already archived, has active movements, or changed concurrently.</response>
    [HttpPost("documents/{documentId:guid}/archive")]
    [EndpointSummary("Archives a registered document at an archive location.")]
    [EndpointDescription("Archives a registered document at an archive location. An archived document rejects changes until it is unarchived. Requires the DOCUMENT_ARCHIVE permission and MANAGE access on the document. Responses: 200 (The archived document.), 400 (The location is unknown or inactive, or the reason is too long.), 403 (The caller lacks DOCUMENT_ARCHIVE or MANAGE access on this document.), 404 (No document with that id exists.), 409 (The document is not registered, is already archived, has active movements, or changed concurrently.)")]
    [Authorize(Policy = Permissions.DocumentArchive)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] ArchiveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ArchiveDocumentCommand(documentId, userId, request.ArchiveLocationId, request.Reason),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Restores an archived document to the status it held before archiving.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason, at most 500 characters (string, required). Example: <c>Required for a court review.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The restored document.</response>
    /// <response code="400">The reason is missing or too long.</response>
    /// <response code="403">The caller lacks DOCUMENT_ARCHIVE or MANAGE access on this document.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not archived or changed concurrently.</response>
    [HttpPost("documents/{documentId:guid}/unarchive")]
    [EndpointSummary("Restores an archived document to the status it held before archiving.")]
    [EndpointDescription("Restores an archived document to the status it held before archiving. Requires the DOCUMENT_ARCHIVE permission and MANAGE access on the document. Responses: 200 (The restored document.), 400 (The reason is missing or too long.), 403 (The caller lacks DOCUMENT_ARCHIVE or MANAGE access on this document.), 404 (No document with that id exists.), 409 (The document is not archived or changed concurrently.)")]
    [Authorize(Policy = Permissions.DocumentArchive)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unarchive(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] UnarchiveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new UnarchiveDocumentCommand(documentId, userId, request.Reason), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
