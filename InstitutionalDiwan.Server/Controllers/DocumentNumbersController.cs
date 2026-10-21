using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Numbering;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Official number lifecycle for a document: issue (gap-free allocation), cancel with a reason, and
/// replace with a linked correction. Writes require <c>NUMBERING_MANAGE</c>; history requires
/// <c>NUMBERING_VIEW</c>.
/// </summary>
[ApiController]
[Route("api/documents/{documentId:guid}/numbers")]
[Produces("application/json")]
public sealed class DocumentNumbersController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentNumbersController(ISender sender) => _sender = sender;

    /// <summary>Lists every official number issued for the document, with its event history.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The number history, oldest first.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("history")]
    [EndpointSummary("Lists every official number issued for the document, with its event history.")]
    [EndpointDescription("Lists every official number issued for the document, with its event history. Requires the NUMBERING_VIEW permission. Responses: 200 (The number history, oldest first.), 404 (No document with that id exists.)")]
    [Authorize(Policy = Permissions.NumberingView)]
    [ProducesResponseType<IReadOnlyCollection<DocumentNumberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory([Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDocumentNumberHistoryQuery(documentId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the document's central number together with every department register line, each
    /// with its dated registration events.
    /// </summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The central number and department entries.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("registry-entries")]
    [EndpointSummary("Returns the document's central number together with every department register line, each with its dated registration events.")]
    [EndpointDescription("Returns the document's central number together with every department register line, each with its dated registration events. Requires the NUMBERING_VIEW permission. Responses: 200 (The central number and department entries.), 404 (No document with that id exists.)")]
    [Authorize(Policy = Permissions.NumberingView)]
    [ProducesResponseType<DocumentRegistryNumbersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistryEntries([Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDocumentRegistryNumbersQuery(documentId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Records the number and date a department wrote in its own register. The central Diwan number
    /// is not changed. Repeating the call for the same department keeps the number and appends a new
    /// dated entry.
    /// </summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>departmentId</term><description>Department unique identifier (GUID, required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>registryNumberText</term><description>Department register number text (string, required). Example: <c>FIN-001/2026</c>.</description></item>
    /// <item><term>registryDate</term><description>Registry date (date, required). Example: <c>2026-01-15</c>.</description></item>
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department register entry.</response>
    /// <response code="400">The number or date is missing or invalid.</response>
    /// <response code="404">No document or department with that id exists.</response>
    /// <response code="409">The document has no central number yet, the department has no active
    /// register, the number is taken for that year, or a different number was already recorded.</response>
    [HttpPost("department-registration")]
    [EndpointSummary("Records the number and date a department wrote in its own register. The central Diwan number is not changed. Repeating the call for the same department keeps the number and appends a new dated entry.")]
    [EndpointDescription("Records the number and date a department wrote in its own register. The central Diwan number is not changed. Repeating the call for the same department keeps the number and appends a new dated entry. Requires the NUMBERING_MANAGE permission. Responses: 200 (The department register entry.), 400 (The number or date is missing or invalid.), 404 (No document or department with that id exists.), 409 (The document has no central number yet, the department has no active register, the number is taken for that year, or a different number was already recorded.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordDepartmentRegistration(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] RecordDepartmentRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new RecordDepartmentRegistrationCommand(
            documentId,
            request.DepartmentId,
            request.RegistryNumberText,
            request.RegistryDate,
            userId,
            request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Issues the next official number for a DRAFT document and registers it.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>numberingYear</term><description>Numbering year (integer, optional). Example: <c>2026</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The issued number.</response>
    /// <response code="400">The numbering year is invalid.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not a draft, already numbered, or the number is taken.</response>
    [HttpPost("issue")]
    [EndpointSummary("Issues the next official number for a DRAFT document and registers it.")]
    [EndpointDescription("Issues the next official number for a DRAFT document and registers it. Requires the NUMBERING_MANAGE permission. Responses: 200 (The issued number.), 400 (The numbering year is invalid.), 404 (No document with that id exists.), 409 (The document is not a draft, already numbered, or the number is taken.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] IssueOfficialNumberRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new IssueOfficialNumberCommand(documentId, userId, request.NumberingYear), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Cancels an active official number with a required reason.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="documentNumberId">Document number unique identifier (GUID, route). Example: <c>e5c8a3d9-4f72-4b16-9a3d-8e1b6c7f0a92</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The cancelled number.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="404">No document or number with that id exists.</response>
    /// <response code="409">The number is not active.</response>
    [HttpPost("{documentNumberId:guid}/cancel")]
    [EndpointSummary("Cancels an active official number with a required reason.")]
    [EndpointDescription("Cancels an active official number with a required reason. Requires the NUMBERING_MANAGE permission. Responses: 200 (The cancelled number.), 400 (The reason is missing.), 404 (No document or number with that id exists.), 409 (The number is not active.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Document number unique identifier (GUID, route). Example: e5c8a3d9-4f72-4b16-9a3d-8e1b6c7f0a92.")] Guid documentNumberId,
        [FromBody] CancelOfficialNumberRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new CancelOfficialNumberCommand(
            documentId, documentNumberId, userId, request.Reason), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Replaces an active official number: allocates a new sequence, links the old number to its
    /// replacement, and re-points the document at the new number.
    /// </summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="documentNumberId">Document number unique identifier (GUID, route). Example: <c>e5c8a3d9-4f72-4b16-9a3d-8e1b6c7f0a92</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// <item><term>numberingYear</term><description>Numbering year (integer, optional). Example: <c>2026</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The replacement number.</response>
    /// <response code="400">The reason or numbering year is invalid.</response>
    /// <response code="404">No document or number with that id exists.</response>
    /// <response code="409">The number is not active, or the replacement number is taken.</response>
    [HttpPost("{documentNumberId:guid}/replace")]
    [EndpointSummary("Replaces an active official number: allocates a new sequence, links the old number to its replacement, and re-points the document at the new number.")]
    [EndpointDescription("Replaces an active official number: allocates a new sequence, links the old number to its replacement, and re-points the document at the new number. Requires the NUMBERING_MANAGE permission. Responses: 200 (The replacement number.), 400 (The reason or numbering year is invalid.), 404 (No document or number with that id exists.), 409 (The number is not active, or the replacement number is taken.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Replace(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Document number unique identifier (GUID, route). Example: e5c8a3d9-4f72-4b16-9a3d-8e1b6c7f0a92.")] Guid documentNumberId,
        [FromBody] ReplaceOfficialNumberRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new ReplaceOfficialNumberCommand(
            documentId, documentNumberId, userId, request.Reason, request.NumberingYear),
            cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
