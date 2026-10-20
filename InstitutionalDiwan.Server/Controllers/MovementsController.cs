using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Movements;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Document movement transitions. Receiving is gated: the destination department must have recorded
/// its own register number for the document before it can take custody.
/// </summary>
[ApiController]
[Route("api/movements")]
[Produces("application/json")]
public sealed class MovementsController : ControllerBase
{
    private readonly ISender _sender;

    public MovementsController(ISender sender) => _sender = sender;

    /// <summary>Receives the movement's current step at the destination department.</summary>
    /// <response code="200">The recorded receipt.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not awaiting receipt, or the department's register entry
    /// is missing — record it first.</response>
    [HttpPost("{movementId:guid}/receive")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementStepReceiptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(
        Guid movementId,
        [FromBody] ReceiveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ReceiveDocumentCommand(movementId, userId, request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
