using System.Security.Claims;
using InstitutionalDiwan.Application.Features.Identity.AccessGrants;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="200">The access-grant list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the ACCESS_GRANT_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DocumentAccessGrantResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentAccessGrantsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a user an access level on a document, recording the caller as the grantor.</summary>
    /// <response code="200">The created access grant.</response>
    /// <response code="400">The payload failed validation, or the document/user does not exist.</response>
    [HttpPost]
    [ProducesResponseType<DocumentAccessGrantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Grant(
        Guid documentId,
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
    /// <response code="200">The revoked access grant.</response>
    /// <response code="404">No access grant with that id exists.</response>
    /// <response code="409">The access grant was already revoked.</response>
    [HttpDelete("{grantId:guid}")]
    [ProducesResponseType<DocumentAccessGrantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(
        Guid documentId,
        Guid grantId,
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
