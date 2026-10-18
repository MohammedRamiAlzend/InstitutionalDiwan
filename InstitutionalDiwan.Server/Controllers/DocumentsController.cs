using System.Security.Claims;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/documents")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class DocumentsController : ControllerBase
{
    private readonly ISender _sender;

    public DocumentsController(ISender sender) => _sender = sender;

    /// <summary>Lists the most recent documents, newest first.</summary>
    /// <response code="200">The document list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a document by id.</summary>
    /// <response code="200">The document.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentByIdQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new document in the caller's department with the supplied official number.</summary>
    /// <remarks>
    /// The document is persisted as REGISTERED when the official number + numbering year pair is free.
    /// If the pair is already taken the endpoint responds with <c>409 Conflict</c>.
    /// </remarks>
    /// <response code="200">The created document.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="403">The token lacks the DOCUMENT_CREATE permission.</response>
    /// <response code="409">The official number is already taken for that numbering year.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.DocumentCreate)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _sender.Send(new CreateDocumentCommand(
            userId,
            request.Title,
            request.DocumentTypeId,
            request.DirectionId,
            request.OfficialNumberText,
            request.NumberingYear,
            request.SubjectSummary,
            request.ConfidentialityLevelId,
            request.PriorityLevelId), cancellationToken);

        return result.ToActionResult();
    }
}
