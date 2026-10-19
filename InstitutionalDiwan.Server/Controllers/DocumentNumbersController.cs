using System.Security.Claims;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Numbering;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="200">The number history, oldest first.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("history")]
    [Authorize(Policy = Permissions.NumberingView)]
    [ProducesResponseType<IReadOnlyCollection<DocumentNumberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDocumentNumberHistoryQuery(documentId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Issues the next official number for a DRAFT document and registers it.</summary>
    /// <response code="200">The issued number.</response>
    /// <response code="400">The numbering year is invalid.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not a draft, already numbered, or the number is taken.</response>
    [HttpPost("issue")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(
        Guid documentId,
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
    /// <response code="200">The cancelled number.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="404">No document or number with that id exists.</response>
    /// <response code="409">The number is not active.</response>
    [HttpPost("{documentNumberId:guid}/cancel")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid documentId,
        Guid documentNumberId,
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
    /// <response code="200">The replacement number.</response>
    /// <response code="400">The reason or numbering year is invalid.</response>
    /// <response code="404">No document or number with that id exists.</response>
    /// <response code="409">The number is not active, or the replacement number is taken.</response>
    [HttpPost("{documentNumberId:guid}/replace")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<DocumentNumberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Replace(
        Guid documentId,
        Guid documentNumberId,
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
