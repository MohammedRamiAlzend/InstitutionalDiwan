using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

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
    /// <remarks>
    /// Returns all documents ordered by most recent first. Requires the <c>DOCUMENT_VIEW</c> permission.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document list returned as <c>IReadOnlyCollection&lt;DocumentResponse&gt;</c>.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [EndpointSummary("Lists the most recent documents, newest first.")]
    [EndpointDescription("Lists the most recent documents, newest first. Returns all documents ordered by most recent first. Requires the DOCUMENT_VIEW permission. Responses: 200 (The document list returned as IReadOnlyCollection<DocumentResponse>.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [Produces("application/json", Type = typeof(IReadOnlyCollection<DocumentResponse>))]
    [ProducesResponseType<IReadOnlyCollection<DocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDocumentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a document by id.</summary>
    /// <remarks>
    /// Retrieves a specific document by its unique identifier.
    /// </remarks>
    /// <param name="documentId">The unique identifier of the document (GUID). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c></param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document returned as <c>DocumentResponse</c>.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}")]
    [EndpointSummary("Fetches a document by id.")]
    [EndpointDescription("Fetches a document by id. Retrieves a specific document by its unique identifier. Requires the DOCUMENT_VIEW permission. Responses: 200 (The document returned as DocumentResponse.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.), 404 (No document with that id exists.)")]
    [Produces("application/json", Type = typeof(DocumentResponse))]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("Document unique identifier (GUID). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentByIdQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a document with its participants and status history.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document detail.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}/details")]
    [EndpointSummary("Fetches a document with its participants and status history.")]
    [EndpointDescription("Fetches a document with its participants and status history. Requires the DOCUMENT_VIEW permission. Responses: 200 (The document detail.), 404 (No document with that id exists.)")]
    [ProducesResponseType<DocumentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails([Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentDetailsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the document's audit timeline (oldest first): status transitions, added participants,
    /// and official-number events.
    /// </summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The timeline entries.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("{documentId:guid}/timeline")]
    [EndpointSummary("Returns the document's audit timeline (oldest first): status transitions, added participants, and official-number events.")]
    [EndpointDescription("Returns the document's audit timeline (oldest first): status transitions, added participants, and official-number events. Requires the DOCUMENT_VIEW permission. Responses: 200 (The timeline entries.), 404 (No document with that id exists.)")]
    [ProducesResponseType<IReadOnlyCollection<DocumentTimelineEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline([Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentTimelineQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a DRAFT document without consuming an official number.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>title</term><description>Document title (string, required). Example: <c>طلب تصريح زيارة لوفدة</c>.</description></item>
    /// <item><term>documentTypeId</term><description>Document type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>directionId</term><description>Direction id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>subjectSummary</term><description>Subject summary (string, optional). Example: <c>طلب تصريح زيارة لوفدة خارجية</c>.</description></item>
    /// <item><term>confidentialityLevelId</term><description>Confidentiality level id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>priorityLevelId</term><description>Priority level id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>documentDate</term><description>Document date (date, optional). Example: <c>2026-01-15</c>.</description></item>
    /// <item><term>externalReferenceNumber</term><description>External reference number (string, optional). Example: <c>EXT-2026-114</c>.</description></item>
    /// <item><term>externalDocumentDate</term><description>External document date (date, optional). Example: <c>2026-01-10</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created draft.</response>
    /// <response code="400">The payload failed validation or referenced an unknown/inactive document type.</response>
    /// <response code="403">The token lacks the DOCUMENT_CREATE permission.</response>
    [HttpPost("drafts")]
    [EndpointSummary("Creates a DRAFT document without consuming an official number.")]
    [EndpointDescription("Creates a DRAFT document without consuming an official number. Requires the DOCUMENT_CREATE permission. Responses: 200 (The created draft.), 400 (The payload failed validation or referenced an unknown/inactive document type.), 403 (The token lacks the DOCUMENT_CREATE permission.)")]
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
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>title</term><description>Document title (string, required). Example: <c>طلب تصريح زيارة لوفدة</c>.</description></item>
    /// <item><term>subjectSummary</term><description>Subject summary (string, optional). Example: <c>طلب تصريح زيارة لوفدة خارجية</c>.</description></item>
    /// <item><term>confidentialityLevelId</term><description>Confidentiality level id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>priorityLevelId</term><description>Priority level id (integer, optional). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated document.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is no longer in DRAFT status.</response>
    [HttpPut("{documentId:guid}")]
    [EndpointSummary("Updates a DRAFT document's core metadata.")]
    [EndpointDescription("Updates a DRAFT document's core metadata. Requires the DOCUMENT_EDIT permission. Responses: 200 (The updated document.), 400 (The payload failed validation.), 404 (No document with that id exists.), 409 (The document is no longer in DRAFT status.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
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
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>numberingYear</term><description>Numbering year (integer, optional). Example: <c>2026</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The registered document.</response>
    /// <response code="400">The numbering year is invalid.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is not a draft, already numbered, or the number is taken.</response>
    [HttpPost("{documentId:guid}/register")]
    [EndpointSummary("Registers a DRAFT document: allocates the next official number from the matching registry and moves the document to REGISTERED.")]
    [EndpointDescription("Registers a DRAFT document: allocates the next official number from the matching registry and moves the document to REGISTERED. Requires the DOCUMENT_CREATE permission. Responses: 200 (The registered document.), 400 (The numbering year is invalid.), 404 (No document with that id exists.), 409 (The document is not a draft, already numbered, or the number is taken.)")]
    [Authorize(Policy = Permissions.DocumentCreate)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
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
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>roleId</term><description>Role id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>externalOrganizationId</term><description>External organization unique identifier (GUID, optional). Example: <c>f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53</c>.</description></item>
    /// <item><term>internalDepartmentId</term><description>Internal department unique identifier (GUID, optional). Example: <c>8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12</c>.</description></item>
    /// <item><term>personName</term><description>Person name (string, optional). Example: <c>أحمد محمد علي</c>.</description></item>
    /// <item><term>isPrimary</term><description>Is primary (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created participant.</response>
    /// <response code="400">The payload is invalid or referenced an unknown role/department/organization.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document already has an external participant or is canceled.</response>
    [HttpPost("{documentId:guid}/participants")]
    [EndpointSummary("Adds a participant (external organization, internal department, or person) to a document.")]
    [EndpointDescription("Adds a participant (external organization, internal department, or person) to a document. Requires the DOCUMENT_EDIT permission. Responses: 200 (The created participant.), 400 (The payload is invalid or referenced an unknown role/department/organization.), 404 (No document with that id exists.), 409 (The document already has an external participant or is canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentParticipantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddParticipant(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
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
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The canceled document.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is archived, already canceled, or has active movements.</response>
    [HttpPost("{documentId:guid}/cancel")]
    [EndpointSummary("Cancels a document with a required reason.")]
    [EndpointDescription("Cancels a document with a required reason. Requires the DOCUMENT_EDIT permission. Responses: 200 (The canceled document.), 400 (The reason is missing.), 404 (No document with that id exists.), 409 (The document is archived, already canceled, or has active movements.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
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
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>title</term><description>Document title (string, required). Example: <c>طلب تصريح زيارة لوفدة</c>.</description></item>
    /// <item><term>documentTypeId</term><description>Document type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>directionId</term><description>Direction id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>officialNumberText</term><description>Official number text (string, required). Example: <c>1447/2026</c>.</description></item>
    /// <item><term>numberingYear</term><description>Numbering year (integer, required). Example: <c>2026</c>.</description></item>
    /// <item><term>subjectSummary</term><description>Subject summary (string, optional). Example: <c>طلب تصريح زيارة لوفدة خارجية</c>.</description></item>
    /// <item><term>confidentialityLevelId</term><description>Confidentiality level id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>priorityLevelId</term><description>Priority level id (integer, optional). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created document.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="403">The token lacks the DOCUMENT_CREATE permission.</response>
    /// <response code="409">The official number is already taken for that numbering year.</response>
    [HttpPost]
    [EndpointSummary("Creates a new document in the caller's department with the supplied official number.")]
    [EndpointDescription("Creates a new document in the caller's department with the supplied official number. The document is persisted as REGISTERED when the official number + numbering year pair is free. If the pair is already taken the endpoint responds with 409 Conflict. Requires the DOCUMENT_CREATE permission. Responses: 200 (The created document.), 400 (The payload failed validation.), 403 (The token lacks the DOCUMENT_CREATE permission.), 409 (The official number is already taken for that numbering year.)")]
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
