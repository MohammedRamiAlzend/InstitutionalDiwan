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

    /// <summary>Lists every decision recorded on one document, newest first, with each decision's appeals.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's decisions, newest first. A joined file also includes the principal file's decisions when the caller can view that file.</response>
    /// <response code="403">The caller cannot view the document.</response>
    /// <response code="404">No document has this id.</response>
    [HttpGet("api/documents/{documentId:guid}/decisions")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Lists every decision recorded on one document, newest first, with each decision's appeals.")]
    [EndpointDescription("Lists every decision recorded on one document, newest first, with each decision's appeals. When the document was joined into a principal file, that file's decisions are included for reading only if the caller can view the principal file. Requires the DOCUMENT_VIEW permission and view access to the document. Responses: 200 (The document's decisions, newest first. A joined file also includes the principal file's decisions when the caller can view that file.), 403 (The caller cannot view the document.), 404 (No document has this id.)")]
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

    /// <summary>Lists other unarchived files of the same type that share a person with this document.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">Files the caller may view. An empty list means nothing to suggest. The list is not a join.</response>
    /// <response code="403">The caller cannot view the document.</response>
    /// <response code="404">No document has this id.</response>
    [HttpGet("api/documents/{documentId:guid}/join-candidates")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Lists other unarchived files of the same type that share a person with this document.")]
    [EndpointDescription("Lists other unarchived files of the same type that share a person with this document and that the caller may view. Canceled files are omitted. The list is a suggestion, not a join. Requires the DOCUMENT_VIEW permission and view access to the document. Responses: 200 (Files the caller may view. An empty list means nothing to suggest. The list is not a join.), 403 (The caller cannot view the document.), 404 (No document has this id.)")]
    [ProducesResponseType<IReadOnlyList<JoinCandidateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinCandidates(
        [Description("Document id (GUID, route). Example: 3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListJoinCandidatesQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns the one suggested next step: refer, send for archive, archive in place, move a joined file, or none.</summary>
    /// <param name="documentId">Document id (GUID, route). Example: <c>3f1c0a55-6e42-4b18-9d77-2a8c4e91b0d6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The suggested step. Kind is null when nothing should be offered.</response>
    /// <response code="403">The caller cannot view the document.</response>
    /// <response code="404">No document has this id.</response>
    [HttpGet("api/documents/{documentId:guid}/next-action")]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Returns the one suggested next step: refer, send for archive, archive in place, move a joined file, or none.")]
    [EndpointDescription("Returns the one suggested next step: refer, send for archive, archive in place, move a joined file, or none. A join suggests an ordinary movement only when the principal file is held by a different department and the caller can view that file. The step is a suggestion; the existing movement and archive commands still perform it. Requires the DOCUMENT_VIEW permission and view access to the document. Responses: 200 (The suggested step. Kind is null when nothing should be offered.), 403 (The caller cannot view the document.), 404 (No document has this id.)")]
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
    /// <item><term>joinTargetDocumentId</term><description>The principal file when the decision type's effect is unification (GUID, required for that effect, otherwise omit). Example: <c>6b2d8f40-1c55-4e19-8a30-7d4c9b52e1f0</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The issued decision. A non-appealable type, including unification, is born final. Unification also links the file to the principal file.</response>
    /// <response code="401">The token has no user id.</response>
    /// <response code="403">The caller's department does not hold the document.</response>
    /// <response code="409">Another decision is in force, the number is taken, the type cannot be used, the file is already joined, or the principal file is not a valid unification target.</response>
    [HttpPost("api/documents/{documentId:guid}/decisions")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Issues a decision from the department that currently holds the document.")]
    [EndpointDescription("Issues a decision from the department that currently holds the document. A unification decision names the principal file and links this file into it without changing this file's number, status, or location. Requires the DECISION_MANAGE permission and an unlocked department-register visit in the caller's scope. Responses: 200 (The issued decision. A non-appealable type, including unification, is born final. Unification also links the file to the principal file.), 401 (The token has no user id.), 403 (The caller's department does not hold the document.), 409 (Another decision is in force, the number is taken, the type cannot be used, the file is already joined, or the principal file is not a valid unification target.)")]
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
            request.DocumentFileId,
            request.JoinTargetDocumentId), cancellationToken);
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

    /// <summary>Cancels a mistaken decision. Only a system-scoped administrator may cancel one. Canceling a unification also removes the join link.</summary>
    /// <param name="decisionId">Decision id (GUID, route). Example: <c>b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Why the entry is canceled (string, required, at most 1000). Example: <c>أُدخل الرقم على الوثيقة الخطأ</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The canceled decision. A unification's join link is removed in the same save.</response>
    /// <response code="403">The caller is not a system-scoped administrator.</response>
    /// <response code="404">No decision has this id, or it was already canceled.</response>
    /// <response code="409">The reason is empty.</response>
    [HttpPost("api/decisions/{decisionId:guid}/cancel")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Cancels a mistaken decision. Only a system-scoped administrator may cancel one. Canceling a unification also removes the join link.")]
    [EndpointDescription("Cancels a mistaken decision. Only a system-scoped administrator may cancel one, and a reason is required. Canceling a unification removes the JoinedInto link in the same save. Requires the DECISION_MANAGE permission. Responses: 200 (The canceled decision. A unification's join link is removed in the same save.), 403 (The caller is not a system-scoped administrator.), 404 (No decision has this id, or it was already canceled.), 409 (The reason is empty.)")]
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

    /// <summary>Records an appeal on an issued, appealable decision. The issuing department or the appellant department may record it.</summary>
    /// <param name="decisionId">Decision id (GUID, route). Example: <c>b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>appealDate</term><description>Appeal date (date, required). Example: <c>2026-11-11</c>.</description></item>
    /// <item><term>appellantDepartmentId</term><description>Appellant department (GUID, optional). When omitted, the document's owning department is used. Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>referenceNumber</term><description>Optional reference number (string, at most 100). Example: <c>ط/2026/4</c>.</description></item>
    /// <item><term>notes</term><description>Optional notes (string, at most 2000). Example: <c>طعن النيابة على القرار</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The decision, now appealed, including the pending appeal.</response>
    /// <response code="400">The date is not a valid year, or the reference or the notes are too long.</response>
    /// <response code="401">The token has no user id.</response>
    /// <response code="403">The caller's scope covers neither the issuing department nor the appellant department.</response>
    /// <response code="404">No decision or appellant department has this id.</response>
    /// <response code="409">The decision cannot be appealed, or an appeal is already pending.</response>
    [HttpPost("api/decisions/{decisionId:guid}/appeals")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Records an appeal on an issued, appealable decision. The issuing department or the appellant department may record it.")]
    [EndpointDescription("Records an appeal on an issued, appealable decision. The issuing department or the appellant department may record it. Requires the DECISION_MANAGE permission. The document may already be archived; recording the appeal does not move it and does not open a movement. Responses: 200 (The decision, now appealed, including the pending appeal.), 400 (The date is not a valid year, or the reference or the notes are too long.), 401 (The token has no user id.), 403 (The caller's scope covers neither the issuing department nor the appellant department.), 404 (No decision or appellant department has this id.), 409 (The decision cannot be appealed, or an appeal is already pending.)")]
    [ProducesResponseType<DocumentDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordAppeal(
        [Description("Decision id (GUID, route). Example: b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4.")] Guid decisionId,
        [FromBody] RecordDecisionAppealRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new RecordDecisionAppealCommand(
            decisionId,
            userId,
            request.AppealDate,
            request.AppellantDepartmentId,
            request.ReferenceNumber,
            request.Notes), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Records the outcome of a pending appeal: accepted revokes the decision, rejected makes it final.</summary>
    /// <param name="decisionId">Decision id (GUID, route). Example: <c>b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4</c>.</param>
    /// <param name="appealId">Appeal id (GUID, route). Example: <c>c31f7d90-2e55-4b10-9a66-4d8e0f13b2c5</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>outcomeId</term><description>Ruling: 2 accepted or 3 rejected (integer, required). Example: <c>2</c>.</description></item>
    /// <item><term>outcomeDate</term><description>Date of the ruling (date, required). Example: <c>2026-11-12</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The decision after the ruling. Accepted is revoked; rejected is final.</response>
    /// <response code="400">The outcome is not accepted or rejected, the appeal is not pending, or the date is not a valid year.</response>
    /// <response code="401">The token has no user id.</response>
    /// <response code="403">The caller's scope covers neither the issuing department nor the appellant department.</response>
    /// <response code="404">No decision or appeal has this id.</response>
    [HttpPut("api/decisions/{decisionId:guid}/appeals/{appealId:guid}/outcome")]
    [Authorize(Policy = Permissions.DecisionManage)]
    [EndpointSummary("Records the outcome of a pending appeal: accepted revokes the decision, rejected makes it final.")]
    [EndpointDescription("Records the outcome of a pending appeal: accepted revokes the decision, rejected makes it final. Requires the DECISION_MANAGE permission. The issuing department or the appellant department may record it, including when the document is archived. Responses: 200 (The decision after the ruling. Accepted is revoked; rejected is final.), 400 (The outcome is not accepted or rejected, the appeal is not pending, or the date is not a valid year.), 401 (The token has no user id.), 403 (The caller's scope covers neither the issuing department nor the appellant department.), 404 (No decision or appeal has this id.)")]
    [ProducesResponseType<DocumentDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordAppealOutcome(
        [Description("Decision id (GUID, route). Example: b21e6c80-1d44-4a09-8f55-3c7d9e02a1b4.")] Guid decisionId,
        [Description("Appeal id (GUID, route). Example: c31f7d90-2e55-4b10-9a66-4d8e0f13b2c5.")] Guid appealId,
        [FromBody] RecordAppealOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new RecordAppealOutcomeCommand(
            decisionId,
            appealId,
            userId,
            request.OutcomeId,
            request.OutcomeDate), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
