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

    /// <summary>Fetches a document with its participants and status history.</summary>
    /// <response code="200">The document detail.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}/details")]
    [ProducesResponseType<DocumentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentDetailsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the document's audit timeline (oldest first): status transitions, added participants,
    /// and official-number events.
    /// </summary>
    /// <response code="200">The timeline entries.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}/timeline")]
    [ProducesResponseType<IReadOnlyCollection<DocumentTimelineEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentTimelineQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a DRAFT document without consuming an official number.</summary>
    /// <response code="200">The created draft.</response>
    /// <response code="400">The payload failed validation or referenced an unknown/inactive document type.</response>
    /// <response code="403">The token lacks the DOCUMENT_CREATE permission.</response>
    [HttpPost("drafts")]
    [Authorize(Policy = Permissions.DocumentCreate)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateDraftDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new CreateDraftDocumentCommand(
            userId,
            request.Title,
            request.DocumentTypeId,
            request.DirectionId,
            request.SubjectSummary,
            request.ConfidentialityLevelId,
            request.PriorityLevelId,
            request.DocumentDate,
            request.ExternalReferenceNumber,
            request.ExternalDocumentDate), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a DRAFT document's core metadata.</summary>
    /// <response code="200">The updated document.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is no longer in DRAFT status.</response>
    [HttpPut("{documentId:guid}")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(
        Guid documentId,
        [FromBody] UpdateDocumentMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateDraftMetadataCommand(
            documentId,
            request.Title,
            request.SubjectSummary,
            request.ConfidentialityLevelId,
            request.PriorityLevelId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Registers a DRAFT document: allocates the next official number from the matching registry and
    /// moves the document to REGISTERED.
    /// </summary>
    /// <response code="200">The registered document.</response>
    /// <response code="400">The numbering year is invalid.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not a draft, already numbered, or the number is taken.</response>
    [HttpPost("{documentId:guid}/register")]
    [Authorize(Policy = Permissions.DocumentCreate)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        Guid documentId,
        [FromBody] RegisterDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RegisterDocumentCommand(documentId, userId, request.NumberingYear), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Adds a participant (external organization, internal department, or person) to a document.</summary>
    /// <response code="200">The created participant.</response>
    /// <response code="400">The payload is invalid or referenced an unknown role/department/organization.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document already has an external participant or is canceled.</response>
    [HttpPost("{documentId:guid}/participants")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentParticipantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddParticipant(
        Guid documentId,
        [FromBody] AddDocumentParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AddDocumentParticipantCommand(
            documentId,
            request.RoleId,
            request.ExternalOrganizationId,
            request.InternalDepartmentId,
            request.PersonName,
            request.IsPrimary,
            request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Cancels a document with a required reason.</summary>
    /// <response code="200">The canceled document.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is archived, already canceled, or has active movements.</response>
    [HttpPost("{documentId:guid}/cancel")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid documentId,
        [FromBody] CancelDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CancelDocumentCommand(documentId, userId, request.Reason), cancellationToken);

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
        if (CurrentUserId is not { } userId)
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

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
