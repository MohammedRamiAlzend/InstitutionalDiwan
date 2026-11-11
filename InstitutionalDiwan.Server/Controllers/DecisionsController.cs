using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Decisions;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Decisions;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Decisions issued on documents, and the one next step each in-force decision suggests.</summary>
[ApiController]
[Produces("application/json")]
[Authorize]
public sealed class DecisionsController : ControllerBase
{
    private readonly ISender _sender;

    public DecisionsController(ISender sender) => _sender = sender;

    /// <summary>Lists a page of decisions on documents the caller can view.</summary>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>1</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Free-text search across the decision number, document name, and official number (string, optional). Example: <c>2026/14</c>.</param>
    /// <param name="sortBy">Sort key: <c>decisionDate</c>, <c>decisionNumber</c>, <c>status</c>, or <c>type</c> (string, optional). Example: <c>decisionDate</c>.</param>
    /// <param name="sortDescending">Sort descending when true (boolean, optional). Example: <c>true</c>.</param>
    /// <param name="decisionTypeId">Filter by decision type id (GUID, optional). Example: <c>a1000004-0000-4000-8000-000000000004</c>.</param>
    /// <param name="statusId">Filter by status id: 1 issued, 2 appealed, 3 final, 4 revoked, 5 canceled (integer, optional). Example: <c>3</c>.</param>
    /// <param name="issuingDepartmentId">Filter by the department that issued the decision (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="year">Filter by the decision year (integer, optional). Example: <c>2026</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of decisions the caller is allowed to see.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet("api/decisions")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Lists a page of decisions on documents the caller can view.")]
    [EndpointDescription("Lists a page of decisions on documents the caller can view. Search covers the decision number, the document name, and the official number. Requires the DOCUMENT_VIEW permission. Responses: 200 (One page of decisions the caller is allowed to see.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [ProducesResponseType<PagedResult<DecisionRegisterItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Page number (integer, optional, 1-based). Example: 1.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Free-text search across the decision number, document name, and official number (string, optional). Example: 2026/14.")] [FromQuery] string? search = null,
        [Description("Sort key: decisionDate, decisionNumber, status, or type (string, optional). Example: decisionDate.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        [Description("Filter by decision type id (GUID, optional). Example: a1000004-0000-4000-8000-000000000004.")] [FromQuery] Guid? decisionTypeId = null,
        [Description("Filter by status id: 1 issued, 2 appealed, 3 final, 4 revoked, 5 canceled (integer, optional). Example: 3.")] [FromQuery] int? statusId = null,
        [Description("Filter by the department that issued the decision (GUID, optional). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] [FromQuery] Guid? issuingDepartmentId = null,
        [Description("Filter by the decision year (integer, optional). Example: 2026.")] [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListDecisionsQuery(
            page, pageSize, search, sortBy, sortDescending,
            decisionTypeId, statusId, issuingDepartmentId, year), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists every decision recorded on one document. The appeals list is empty until appeals exist.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's decisions, newest first.</response>
    /// <response code="403">The caller cannot view the document.</response>
    /// <response code="404">No document has this id.</response>
    [HttpGet("api/documents/{documentId:guid}/decisions")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Lists every decision recorded on one document. The appeals list is empty until appeals exist.")]
    [EndpointDescription("Lists every decision recorded on one document. The appeals list is empty until appeals exist. Requires the DOCUMENT_VIEW permission and view access to the document. Responses: 200 (The document's decisions, newest first.), 403 (The caller cannot view the document.), 404 (No document has this id.)")]
    [ProducesResponseType<IReadOnlyList<DocumentDecisionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByDocument(
        [Description("Document id (GUID, route). Example: 3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentDecisionsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns the one suggested next step: refer, send for archive, archive in place, or none.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The suggested step. Kind is null when nothing should be offered.</response>
    /// <response code="403">The caller cannot view the document.</response>
    /// <response code="404">No document has this id.</response>
    [HttpGet("api/documents/{documentId:guid}/next-action")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Returns the one suggested next step: refer, send for archive, archive in place, or none.")]
    [EndpointDescription("Returns the one suggested next step: refer, send for archive, archive in place, or none. The step is a suggestion; the existing movement and archive commands still perform it. Requires the DOCUMENT_VIEW permission and view access to the document. Responses: 200 (The suggested step. Kind is null when nothing should be offered.), 403 (The caller cannot view the document.), 404 (No document has this id.)")]
    [ProducesResponseType<NextActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NextAction(
        [Description("Document id (GUID, route). Example: 3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentNextActionQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Issues a decision from the department that currently holds the document.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>decisionTypeId</term><description>Active decision type linked to the document's type (GUID, required). Example: <c>a1000004-0000-4000-8000-000000000004</c>.</description></item>
    /// <item><term>decisionNumber</term><description>Decision number as written (string, required, at most 100). Example: <c>2026/14</c>.</description></item>
    /// <item><term>decisionDate</term><description>Decision date (date, required). Example: <c>2026-11-11</c>.</description></item>
    /// <item><term>summary</term><description>Short summary (string, optional, at most 2000). Example: <c>منع محاكمة المدعى عليه</c>.</description></item>
    /// <item><term>documentFileId</term><description>An unarchived decision-type file already on this document (GUID, optional). Example: <c>8c4b1e20-5a77-4d33-9f10-6e2b8a41c9d0</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The issued decision. A non-appealable type is born final.</response>
    /// <response code="401">The token has no user id.</response>
    /// <response code="403">The caller's department does not hold the document.</response>
    /// <response code="409">Another decision is in force, the number is taken, the type cannot be used, or unification is not available yet.</response>
    [HttpPost("api/documents/{documentId:guid}/decisions")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Issues a decision from the department that currently holds the document.")]
    [EndpointDescription("Issues a decision from the department that currently holds the document. Requires the DECISION_MANAGE permission and an unlocked department-register visit in the caller's scope. Responses: 200 (The issued decision. A non-appealable type is born final.), 401 (The token has no user id.), 403 (The caller's department does not hold the document.), 409 (Another decision is in force, the number is taken, the type cannot be used, or unification is not available yet.)")]
    [ProducesResponseType<DocumentDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(
        [Description("Document id (GUID, route). Example: 3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6.")] Guid documentId,
        [FromBody] IssueDocumentDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new IssueDocumentDecisionCommand(
            documentId,
            userId,
            request.DecisionTypeId,
            request.DecisionNumber,
            request.DecisionDate,
            request.Summary,
            request.DocumentFileId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Links a decision-type file on the same document to an existing decision.</summary>
    /// <param name="decisionId">Decision id (GUID, route). Example: <c>b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>documentFileId</term><description>An unarchived decision-type file on the same document (GUID, required). Example: <c>8c4b1e20-5a77-4d33-9f10-6e2b8a41c9d0</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The decision with the file linked.</response>
    /// <response code="403">The caller's department no longer holds the document.</response>
    /// <response code="404">No decision has this id.</response>
    /// <response code="409">The file is missing, archived, of another type, or belongs to another document.</response>
    [HttpPut("api/decisions/{decisionId:guid}/file")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Links a decision-type file on the same document to an existing decision.")]
    [EndpointDescription("Links a decision-type file on the same document to an existing decision. Requires the DECISION_MANAGE permission and that the caller's department still holds the document. Responses: 200 (The decision with the file linked.), 403 (The caller's department no longer holds the document.), 404 (No decision has this id.), 409 (The file is missing, archived, of another type, or belongs to another document.)")]
    [ProducesResponseType<DocumentDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AttachFile(
        [Description("Decision id (GUID, route). Example: b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4.")] Guid decisionId,
        [FromBody] AttachDecisionFileRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new AttachDecisionFileCommand(decisionId, userId, request.DocumentFileId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Cancels a mistaken decision. Only a system-scoped administrator may cancel one.</summary>
    /// <param name="decisionId">Decision id (GUID, route). Example: <c>b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Why the entry is canceled (string, required, at most 1000). Example: <c>أُدخل الرقم على الوثيقة الخطأ</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The canceled decision.</response>
    /// <response code="403">The caller is not a system-scoped administrator.</response>
    /// <response code="404">No decision has this id, or it was already canceled.</response>
    /// <response code="409">The reason is empty.</response>
    [HttpPost("api/decisions/{decisionId:guid}/cancel")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Cancels a mistaken decision. Only a system-scoped administrator may cancel one.")]
    [EndpointDescription("Cancels a mistaken decision. Only a system-scoped administrator may cancel one, and a reason is required. Requires the DECISION_MANAGE permission. Responses: 200 (The canceled decision.), 403 (The caller is not a system-scoped administrator.), 404 (No decision has this id, or it was already canceled.), 409 (The reason is empty.)")]
    [ProducesResponseType<DocumentDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [Description("Decision id (GUID, route). Example: b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4.")] Guid decisionId,
        [FromBody] CancelDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CancelDecisionCommand(decisionId, userId, request.Reason), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
