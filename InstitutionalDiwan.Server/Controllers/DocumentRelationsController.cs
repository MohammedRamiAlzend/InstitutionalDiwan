using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Relations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Relations;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Module 08: links documents to each other and reads those links.</summary>
[ApiController]
[Route("api/documents/{documentId:guid}/relations")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class DocumentRelationsController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentRelationsController(ISender sender) => _sender = sender;

    /// <summary>Lists a document's direct links, in both directions.</summary>
    /// <remarks>
    /// Each link reads from this document: <c>Outgoing</c>, <c>Incoming</c>, or <c>Symmetric</c>. Links to
    /// documents the caller cannot view are left out and counted in <c>hiddenRelationCount</c>.
    /// Requires the <c>DOCUMENT_VIEW</c> permission and VIEW access on the document.
    /// </remarks>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The links the caller may see and the number hidden from them.</response>
    /// <response code="403">The caller lacks DOCUMENT_VIEW or VIEW access on this document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet]
    [EndpointSummary("Lists a document's direct links, in both directions.")]
    [EndpointDescription("Lists a document's direct links, in both directions. Each link reads from this document as Outgoing, Incoming, or Symmetric; links to documents the caller cannot view are left out and counted in hiddenRelationCount. Requires the DOCUMENT_VIEW permission and VIEW access on the document. Responses: 200 (The links the caller may see and the number hidden from them.), 403 (The caller lacks DOCUMENT_VIEW or VIEW access on this document.), 404 (No document with that id exists.)")]
    [ProducesResponseType<DocumentRelationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentRelationsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns the documents reachable from this one through links the caller may see.</summary>
    /// <remarks>
    /// The walk stops at documents the caller cannot view and never continues through them; it is capped
    /// at 100 documents and sets <c>truncated</c> when the cap stopped it.
    /// Requires the <c>DOCUMENT_VIEW</c> permission and VIEW access on the root document.
    /// </remarks>
    /// <param name="documentId">Root document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="depth">Number of hops to follow (integer, optional, 1-3). Example: <c>2</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The nodes, edges and relation types of the graph.</response>
    /// <response code="400">The depth is outside 1 to 3.</response>
    /// <response code="403">The caller lacks DOCUMENT_VIEW or VIEW access on the root document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("graph")]
    [EndpointSummary("Returns the documents reachable from this one through links the caller may see.")]
    [EndpointDescription("Returns the documents reachable from this one through links the caller may see. The walk stops at documents the caller cannot view and never continues through them; it is capped at 100 documents and sets truncated when the cap stopped it. Requires the DOCUMENT_VIEW permission and VIEW access on the root document. Responses: 200 (The nodes, edges and relation types of the graph.), 400 (The depth is outside 1 to 3.), 403 (The caller lacks DOCUMENT_VIEW or VIEW access on the root document.), 404 (No document with that id exists.)")]
    [ProducesResponseType<RelationGraphResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Graph(
        [Description("Root document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Number of hops to follow (integer, optional, 1-3). Example: 2.")] [FromQuery] int depth = 2,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDocumentRelationsGraphQuery(documentId, depth), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns a document's relation history, newest first, including removed links.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>1</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of relation events.</response>
    /// <response code="403">The caller lacks DOCUMENT_VIEW or VIEW access on this document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("events")]
    [EndpointSummary("Returns a document's relation history, newest first, including removed links.")]
    [EndpointDescription("Returns a document's relation history, newest first, including links that were later removed. Requires the DOCUMENT_VIEW permission and VIEW access on the document. Responses: 200 (One page of relation events.), 403 (The caller lacks DOCUMENT_VIEW or VIEW access on this document.), 404 (No document with that id exists.)")]
    [ProducesResponseType<PagedResult<DocumentRelationEventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEvents(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Page number (integer, optional, 1-based). Example: 1.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListDocumentRelationEventsQuery(documentId, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Links this document to another one.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>targetDocumentId</term><description>The document to link to (GUID, required). Example: <c>b3f1c8a2-5d47-4e90-8a16-9c2e7d4f1b63</c>.</description></item>
    /// <item><term>relationTypeId</term><description>Active relation type id: 1 reply to, 2 related, 3 supersedes, 4 duplicate of (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>notes</term><description>Notes, at most 500 characters (string, optional). Example: <c>Answers the 2026 request.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The new link, read from this document.</response>
    /// <response code="400">The relation type is unknown, a link to the same document, or the notes are too long.</response>
    /// <response code="403">The caller lacks DOCUMENT_EDIT, DOWNLOAD access on this document, or VIEW access on the target.</response>
    /// <response code="404">This document or the target does not exist.</response>
    /// <response code="409">This document is archived, a document is cancelled, the relation type is inactive, the link exists, or it would create a cycle.</response>
    [HttpPost]
    [EndpointSummary("Links this document to another one.")]
    [EndpointDescription("Links this document to another one with a relation type. The target may be archived, but this document may not, and neither may be cancelled. Requires the DOCUMENT_EDIT permission, DOWNLOAD access on this document, and VIEW access on the target. Responses: 200 (The new link, read from this document.), 400 (The relation type is unknown, a link to the same document, or the notes are too long.), 403 (The caller lacks DOCUMENT_EDIT, DOWNLOAD access on this document, or VIEW access on the target.), 404 (This document or the target does not exist.), 409 (This document is archived, a document is cancelled, the relation type is inactive, the link exists, or it would create a cycle.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentRelationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] CreateDocumentRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateDocumentRelationCommand(
                documentId, userId, request.TargetDocumentId, request.RelationTypeId, request.Notes),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Removes a link of this document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="relationId">Relation unique identifier (GUID, route). Example: <c>d4a9e7b1-3c68-4f25-b0e2-6a8d1c5f9e37</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason, at most 500 characters (string, required). Example: <c>Linked by mistake.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The link was removed; the removal stays in the relation history.</response>
    /// <response code="400">The reason is missing or too long.</response>
    /// <response code="403">The caller lacks DOCUMENT_EDIT or DOWNLOAD access on this document.</response>
    /// <response code="404">No such document, or no such link on this document.</response>
    /// <response code="409">This document is archived.</response>
    [HttpDelete("{relationId:guid}")]
    [EndpointSummary("Removes a link of this document.")]
    [EndpointDescription("Removes a link of this document. The row is deleted and the removal, with its reason, stays in the relation history. Requires the DOCUMENT_EDIT permission and DOWNLOAD access on this document. Responses: 200 (The link was removed; the removal stays in the relation history.), 400 (The reason is missing or too long.), 403 (The caller lacks DOCUMENT_EDIT or DOWNLOAD access on this document.), 404 (No such document, or no such link on this document.), 409 (This document is archived.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Relation unique identifier (GUID, route). Example: d4a9e7b1-3c68-4f25-b0e2-6a8d1c5f9e37.")] Guid relationId,
        [FromBody] RemoveDocumentRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RemoveDocumentRelationCommand(documentId, relationId, userId, request.Reason),
            cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
