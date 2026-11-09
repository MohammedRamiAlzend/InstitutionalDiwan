using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Persons;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>The people named on one document. Reads require <c>DOCUMENT_VIEW</c>; writes <c>DOCUMENT_EDIT</c>.</summary>
[ApiController]
[Route("api/documents/{documentId:guid}/persons")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class DocumentPersonsController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentPersonsController(ISender sender) => _sender = sender;

    /// <summary>Lists the people named on a document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The people, oldest addition first.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the caller has no VIEW access to the document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet]
    [EndpointSummary("Lists the people named on a document.")]
    [EndpointDescription("Lists the people named on a document with their roles. Requires the DOCUMENT_VIEW permission and VIEW access to the document. Responses: 200 (The people, oldest addition first.), 403 (The token lacks DOCUMENT_VIEW, or the caller has no VIEW access to the document.), 404 (No document with that id exists.)")]
    [ProducesResponseType<IReadOnlyList<DocumentPersonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentPersonsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Names a person on a document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>personId</term><description>An existing person (GUID, required without fullName). Example: <c>5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22</c>.</description></item>
    /// <item><term>fullName</term><description>Name of a new person (string, required without personId). Example: <c>أحمد محمد علي</c>.</description></item>
    /// <item><term>roleId</term><description>Role id (integer, optional, the catalog's default when omitted). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The person as named on the document.</response>
    /// <response code="400">The payload is invalid or the role is unknown.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.</response>
    /// <response code="404">The document or the person does not exist.</response>
    /// <response code="409">The person is already on the document, or the document is archived or canceled.</response>
    [HttpPost]
    [EndpointSummary("Names a person on a document.")]
    [EndpointDescription("Names an existing or new person on a document, with the catalog's default role when none is given. Requires the DOCUMENT_EDIT permission and DOWNLOAD access to the document. Responses: 200 (The person as named on the document.), 400 (The payload is invalid or the role is unknown.), 403 (The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.), 404 (The document or the person does not exist.), 409 (The person is already on the document, or the document is archived or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentPersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] DocumentPersonRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new AddDocumentPersonCommand(documentId, userId, request.ToInput()), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Changes the role a person holds on a document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="documentPersonId">Link unique identifier (GUID, route). Example: <c>9a4c2e18-6b37-4f01-a2d5-3e7f1c8b6d40</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>roleId</term><description>Role id (integer, required). Example: <c>2</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The person with the new role.</response>
    /// <response code="400">The role is unknown or inactive.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.</response>
    /// <response code="404">The document does not exist or does not name this person.</response>
    /// <response code="409">The document is archived or canceled.</response>
    [HttpPut("{documentPersonId:guid}")]
    [EndpointSummary("Changes the role a person holds on a document.")]
    [EndpointDescription("Changes the role a person holds on a document. Requires the DOCUMENT_EDIT permission and DOWNLOAD access to the document. Responses: 200 (The person with the new role.), 400 (The role is unknown or inactive.), 403 (The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.), 404 (The document does not exist or does not name this person.), 409 (The document is archived or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentPersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRole(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Link unique identifier (GUID, route). Example: 9a4c2e18-6b37-4f01-a2d5-3e7f1c8b6d40.")] Guid documentPersonId,
        [FromBody] ChangeDocumentPersonRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ChangeDocumentPersonRoleCommand(documentId, documentPersonId, userId, request.RoleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Takes a person off a document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="documentPersonId">Link unique identifier (GUID, route). Example: <c>9a4c2e18-6b37-4f01-a2d5-3e7f1c8b6d40</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The person is no longer named on the document.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.</response>
    /// <response code="404">The document does not exist or does not name this person.</response>
    /// <response code="409">The document is archived or canceled.</response>
    [HttpDelete("{documentPersonId:guid}")]
    [EndpointSummary("Takes a person off a document.")]
    [EndpointDescription("Takes a person off a document; the person's own record stays. Requires the DOCUMENT_EDIT permission and DOWNLOAD access to the document. Responses: 200 (The person is no longer named on the document.), 403 (The token lacks DOCUMENT_EDIT, or the caller has no DOWNLOAD access to the document.), 404 (The document does not exist or does not name this person.), 409 (The document is archived or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Link unique identifier (GUID, route). Example: 9a4c2e18-6b37-4f01-a2d5-3e7f1c8b6d40.")] Guid documentPersonId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new RemoveDocumentPersonCommand(documentId, documentPersonId, userId), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
