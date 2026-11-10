using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The record each department keeps on its visits of a document: the values of the department's own
/// form for the document type, opened when the department receives the document, editable while the
/// document stays there, and locked when it leaves. A department reads its own records; other
/// departments' records need <c>DEPARTMENT_RECORD_VIEW_ALL</c>, and a locked record can only be
/// corrected with <c>DEPARTMENT_RECORD_CORRECT</c> and a reason.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
public sealed class DepartmentRecordsController : ControllerBase
{
    private const string DocumentIdDoc = "Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.";
    private const string VisitIdDoc = "Register visit unique identifier (GUID, route). Example: 5d1c8e27-4b93-4a60-9f2e-3c7a1b84d590.";

    private readonly ISender _sender;

    public DepartmentRecordsController(ISender sender) => _sender = sender;

    /// <summary>Lists the department records on a document that the caller may read.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department register lines with each visit's record, filtered to what the caller may read.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("documents/{documentId:guid}/department-records")]
    [EndpointSummary("Lists the department records on a document that the caller may read.")]
    [EndpointDescription("Lists each department's register line on a document with the record kept on every visit: the record form version, its values, whether it is locked, and whether the caller may edit or correct it. A department's records are returned to callers whose scope covers that department, and to callers holding DEPARTMENT_RECORD_VIEW_ALL who can view the document; other records are left out. Requires the DOCUMENT_VIEW permission. Responses: 200 (The department register lines with each visit's record, filtered to what the caller may read.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW.), 404 (No document with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<DocumentDepartmentRecordsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description(DocumentIdDoc)] Guid documentId, CancellationToken cancellationToken)
        => (await _sender.Send(new GetDocumentDepartmentRecordsQuery(documentId), cancellationToken)).ToActionResult();

    /// <summary>Replaces the values of an open department record.</summary>
    /// <param name="visitId">Register visit unique identifier (GUID, route). Example: <c>5d1c8e27-4b93-4a60-9f2e-3c7a1b84d590</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>values</term><description>Field key and value as text; a blank value clears the field and required fields must stay filled (array, required). Example: <c>[{"fieldKey":"case_no","value":"77/2026"}]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The visit with its new record values.</response>
    /// <response code="400">A value failed validation, names a field outside the record form, or a required field is empty.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller's scope does not cover the department that keeps the record.</response>
    /// <response code="404">No register visit with that id exists.</response>
    /// <response code="409">The document has left the department so the record is locked, or the visit has no record form.</response>
    [HttpPut("registry-visits/{visitId:guid}/fields")]
    [EndpointSummary("Replaces the values of an open department record.")]
    [EndpointDescription("Replaces the values of the record a department keeps on one visit of a document, validated against the record form version the visit was opened with. Only the department that keeps the record can edit it, and only while the document is still there. Requires the DOCUMENT_EDIT permission and a scope covering the record's department. Responses: 200 (The visit with its new record values.), 400 (A value failed validation, names a field outside the record form, or a required field is empty.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_EDIT, or the caller's scope does not cover the department that keeps the record.), 404 (No register visit with that id exists.), 409 (The document has left the department so the record is locked, or the visit has no record form.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DepartmentRecordVisitResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetValues(
        [Description(VisitIdDoc)] Guid visitId,
        [FromBody] SetVisitRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        return (await _sender.Send(
            new SetVisitRecordCommand(visitId, userId, request.Values.ToInputs() ?? []),
            cancellationToken)).ToActionResult();
    }

    /// <summary>Corrects the values of a locked department record, with a reason.</summary>
    /// <param name="visitId">Register visit unique identifier (GUID, route). Example: <c>5d1c8e27-4b93-4a60-9f2e-3c7a1b84d590</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>values</term><description>Field key and value as text; replaces all values of the record (array, required). Example: <c>[{"fieldKey":"case_no","value":"78/2026"}]</c>.</description></item>
    /// <item><term>reason</term><description>Why the locked record is corrected, up to 1000 characters (string, required). Example: <c>تصحيح رقم القضية بعد مراجعة الأصل</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The visit with its corrected record values.</response>
    /// <response code="400">The reason is missing or a value failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DEPARTMENT_RECORD_CORRECT.</response>
    /// <response code="404">No register visit with that id exists.</response>
    /// <response code="409">The record is still open (edit it instead), or the visit has no record form.</response>
    [HttpPut("registry-visits/{visitId:guid}/fields/correct")]
    [EndpointSummary("Corrects the values of a locked department record, with a reason.")]
    [EndpointDescription("Corrects the record a department kept on a visit after the document left that department. The reason is stored on the visit and the change goes through the tamper-evident audit trail. Requires the DEPARTMENT_RECORD_CORRECT permission. Responses: 200 (The visit with its corrected record values.), 400 (The reason is missing or a value failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DEPARTMENT_RECORD_CORRECT.), 404 (No register visit with that id exists.), 409 (The record is still open (edit it instead), or the visit has no record form.)")]
    [Authorize(Policy = Permissions.DepartmentRecordCorrect)]
    [ProducesResponseType<DepartmentRecordVisitResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Correct(
        [Description(VisitIdDoc)] Guid visitId,
        [FromBody] CorrectVisitRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        return (await _sender.Send(
            new CorrectVisitRecordCommand(visitId, userId, request.Values.ToInputs() ?? [], request.Reason),
            cancellationToken)).ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
