using InstitutionalDiwan.Application.Features.Identity.AccessGrants;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/documents/{documentId:guid}/access-grants")]
[Produces("application/json")]
[Authorize(Policy = Permissions.AccessGrantManage)]
public sealed class DocumentAccessGrantsController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentAccessGrantsController(ISender sender) => _sender = sender;

    /// <summary>Lists every access grant recorded against a document. Requires <c>ACCESS_GRANT_MANAGE</c>.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The access-grant list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the ACCESS_GRANT_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Lists every access grant recorded against a document. Requires ACCESS_GRANT_MANAGE.")]
    [EndpointDescription("Lists every access grant recorded against a document. Requires ACCESS_GRANT_MANAGE. Responses: 200 (The access-grant list.), 401 (The token is missing, expired, or invalid.), 403 (The token does not carry the ACCESS_GRANT_MANAGE permission.)")]
    [ProducesResponseType<IReadOnlyCollection<DocumentAccessGrantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentAccessGrantsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a user an access level on a document, recording the caller as the grantor.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>userId</term><description>User unique identifier (GUID, required). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</description></item>
    /// <item><term>accessLevelId</term><description>Access level id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>reason</term><description>Reason (string, optional). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// <item><term>expiresAt</term><description>Expires at (date, optional). Example: <c>2026-12-31</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created access grant.</response>
    /// <response code="400">The payload failed validation, or the document/user does not exist.</response>
    [HttpPost]
    [EndpointSummary("Grants a user an access level on a document, recording the caller as the grantor.")]
    [EndpointDescription("Grants a user an access level on a document, recording the caller as the grantor. Requires the ACCESS_GRANT_MANAGE permission. Responses: 200 (The created access grant.), 400 (The payload failed validation, or the document/user does not exist.)")]
    [ProducesResponseType<DocumentAccessGrantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Grant(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromBody] GrantDocumentAccessRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var grantedByUserId))
            return Unauthorized();

        var result = await _sender.Send(new GrantDocumentAccessCommand(
            documentId,
            request.UserId,
            request.AccessLevelId,
            grantedByUserId,
            request.Reason,
            request.ExpiresAt), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Revokes a document access grant.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="grantId">Grant unique identifier (GUID, route). Example: <c>c8e5a2f7-6d31-4b90-a4e8-3f7b1d9c6e05</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The revoked access grant.</response>
    /// <response code="404">No access grant with that id exists.</response>
    /// <response code="409">The access grant was already revoked.</response>
    [HttpDelete("{grantId:guid}")]
    [EndpointSummary("Revokes a document access grant.")]
    [EndpointDescription("Revokes a document access grant. Requires the ACCESS_GRANT_MANAGE permission. Responses: 200 (The revoked access grant.), 404 (No access grant with that id exists.), 409 (The access grant was already revoked.)")]
    [ProducesResponseType<DocumentAccessGrantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [Description("Grant unique identifier (GUID, route). Example: c8e5a2f7-6d31-4b90-a4e8-3f7b1d9c6e05.")] Guid grantId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var revokedByUserId))
            return Unauthorized();

        var result = await _sender.Send(
            new RevokeDocumentAccessCommand(grantId, revokedByUserId), cancellationToken);

        return result.ToActionResult();
    }
}
