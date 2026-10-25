using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Movements;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Document movement transitions — the routing workflow between departments. Receiving is gated:
/// the destination department must have recorded its own register number for the document before
/// it can take custody.
/// </summary>
[ApiController]
[Route("api/movements")]
[Produces("application/json")]
public sealed class MovementsController : ControllerBase
{
    private readonly ISender _sender;

    public MovementsController(ISender sender) => _sender = sender;

    /// <summary>Routes a document to another department by opening a movement.</summary>
    /// <remarks>
    /// The source department is the caller's primary department; the destination is the department the
    /// document is handed to. Only a REGISTERED document can be routed, and a document can have one
    /// active movement at a time.
    /// </remarks>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>documentId</term><description>Document id (GUID, required). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</description></item>
    /// <item><term>destinationDepartmentId</term><description>Destination department id (GUID, required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>movementTypeId</term><description>Movement type id (integer, optional, default <c>1</c>). Example: <c>1</c>.</description></item>
    /// <item><term>assignedToUserId</term><description>Assigned user id (GUID, optional). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</description></item>
    /// <item><term>instructions</term><description>Instructions (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// <item><term>dueDate</term><description>Due date (date, optional). Example: <c>2026-01-15</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created movement.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission, or the caller is inactive.</response>
    /// <response code="404">No document, department, or assignee with that id exists.</response>
    /// <response code="409">The document is not REGISTERED, the destination department is inactive, or the document already has an active movement.</response>
    [HttpPost]
    [EndpointSummary("Routes a document to another department by opening a movement.")]
    [EndpointDescription("Routes a document to another department by opening a movement. The source department is the caller's primary department; the destination is the department the document is handed to. Only a REGISTERED document can be routed, and a document can have one active movement at a time. Requires the DOCUMENT_EDIT permission. Responses: 200 (The created movement.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission, or the caller is inactive.), 404 (No document, department, or assignee with that id exists.), 409 (The document is not REGISTERED, the destination department is inactive, or the document already has an active movement.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateMovementCommand(
                request.DocumentId,
                userId,
                request.DestinationDepartmentId,
                request.MovementTypeId,
                request.AssignedToUserId,
                request.Instructions,
                request.DueDate),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Fetches a movement with its steps and audit trail.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The movement with its steps and events.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    [HttpGet("{movementId:guid}")]
    [EndpointSummary("Fetches a movement with its steps and audit trail.")]
    [EndpointDescription("Fetches a movement with its steps and audit trail. Requires the DOCUMENT_VIEW permission. Responses: 200 (The movement with its steps and events.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.), 404 (No movement with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMovementByIdQuery(movementId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists every movement of a document, newest first.</summary>
    /// <remarks>
    /// The routing history of a document: each movement with the steps it went through and the
    /// events recorded against it.
    /// </remarks>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's movements, newest first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet("by-document/{documentId:guid}")]
    [EndpointSummary("Lists every movement of a document, newest first.")]
    [EndpointDescription("Lists every movement of a document, newest first: each movement with the steps it went through and the events recorded against it. Requires the DOCUMENT_VIEW permission. Responses: 200 (The document's movements, newest first.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListByDocument(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListDocumentMovementsQuery(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists the documents waiting in a department's inbox.</summary>
    /// <remarks>
    /// The inbox holds the documents a department received and has not dealt with yet: the current
    /// step of their movement is addressed to that department and is sent or received. Each row
    /// carries the document's title and official number next to the movement and the step.
    /// </remarks>
    /// <param name="departmentId">
    /// Department to read the inbox of (GUID, query, optional). Defaults to the caller's own
    /// primary department; another department can be read only when it is inside the caller's
    /// organizational scope. Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department inbox.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission, the caller is inactive, or the requested department is outside the caller's scope.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("inbox")]
    [EndpointSummary("Lists the documents waiting in a department's inbox.")]
    [EndpointDescription("Lists the documents waiting in a department's inbox: movements whose current step is addressed to that department and is sent or received, so the document still awaits the department's action. Each row carries the document's title and official number next to the movement and the step. The department defaults to the caller's own primary department; another department can be read only when it is inside the caller's organizational scope. Requires the DOCUMENT_VIEW permission. Responses: 200 (The department inbox.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission, the caller is inactive, or the requested department is outside the caller's scope.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementQueueItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inbox(
        [FromQuery, Description("Department to read the inbox of (GUID, optional). Defaults to the caller's own primary department. Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new GetDepartmentInboxQuery(userId, departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists the documents a department sent out and that are still elsewhere.</summary>
    /// <remarks>
    /// The outbox holds the documents a department handed over and that have not come back yet:
    /// the current step of their movement leaves that department and is sent or received. Each row
    /// carries the document's title and official number next to the movement and the step.
    /// </remarks>
    /// <param name="departmentId">
    /// Department to read the outbox of (GUID, query, optional). Defaults to the caller's own
    /// primary department; another department can be read only when it is inside the caller's
    /// organizational scope. Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department outbox.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission, the caller is inactive, or the requested department is outside the caller's scope.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("outbox")]
    [EndpointSummary("Lists the documents a department sent out and that are still elsewhere.")]
    [EndpointDescription("Lists the documents a department sent out and that are still with the receiving side: movements whose current step leaves that department and is sent or received, so the document has not come back yet. Each row carries the document's title and official number next to the movement and the step. The department defaults to the caller's own primary department; another department can be read only when it is inside the caller's organizational scope. Requires the DOCUMENT_VIEW permission. Responses: 200 (The department outbox.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission, the caller is inactive, or the requested department is outside the caller's scope.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementQueueItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Outbox(
        [FromQuery, Description("Department to read the outbox of (GUID, optional). Defaults to the caller's own primary department. Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new GetDepartmentOutboxQuery(userId, departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Sends the movement's current step to the destination department.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The sent movement.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The movement is not in a status that can be sent.</response>
    [HttpPost("{movementId:guid}/send")]
    [EndpointSummary("Sends the movement's current step to the destination department.")]
    [EndpointDescription("Sends the movement's current step to the destination department. Requires the DOCUMENT_EDIT permission. Responses: 200 (The sent movement.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The movement is not in a status that can be sent.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] SendMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new SendMovementCommand(movementId, userId, request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Receives the movement's current step at the destination department.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The recorded receipt.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not awaiting receipt, or the department's register entry
    /// is missing — record it first.</response>
    [HttpPost("{movementId:guid}/receive")]
    [EndpointSummary("Receives the movement's current step at the destination department.")]
    [EndpointDescription("Receives the movement's current step at the destination department. Requires the DOCUMENT_EDIT permission. Responses: 200 (The recorded receipt.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step is not awaiting receipt, or the department's register entry is missing — record it first.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementStepReceiptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] ReceiveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ReceiveDocumentCommand(movementId, userId, request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Completes the movement's current step at the destination department.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Outcome notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The completed movement.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step was not received yet, or the movement is already terminal.</response>
    [HttpPost("{movementId:guid}/complete")]
    [EndpointSummary("Completes the movement's current step at the destination department.")]
    [EndpointDescription("Completes the movement's current step at the destination department. Requires the DOCUMENT_EDIT permission. Responses: 200 (The completed movement.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step was not received yet, or the movement is already terminal.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] CompleteMovementStepRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CompleteMovementStepCommand(movementId, userId, request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Rejects the movement's current step with a required reason.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The rejected movement.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The movement is not in a status that can be rejected.</response>
    [HttpPost("{movementId:guid}/reject")]
    [EndpointSummary("Rejects the movement's current step with a required reason.")]
    [EndpointDescription("Rejects the movement's current step with a required reason. Requires the DOCUMENT_EDIT permission. Responses: 200 (The rejected movement.), 400 (The reason is missing.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The movement is not in a status that can be rejected.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] RejectMovementStepRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RejectMovementStepCommand(movementId, userId, request.Reason), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Returns the document to the sending department with a required reason.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The returned movement.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The movement is not in a status that can be returned.</response>
    [HttpPost("{movementId:guid}/return")]
    [EndpointSummary("Returns the document to the sending department with a required reason.")]
    [EndpointDescription("Returns the document to the sending department with a required reason. Requires the DOCUMENT_EDIT permission. Responses: 200 (The returned movement.), 400 (The reason is missing.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The movement is not in a status that can be returned.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Return(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] ReturnMovementToSenderRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ReturnMovementToSenderCommand(movementId, userId, request.Reason), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Redirects the document to another department without closing the movement.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>newDestinationDepartmentId</term><description>New destination department id (GUID, required). Example: <c>8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12</c>.</description></item>
    /// <item><term>assignedToUserId</term><description>Assigned user id (GUID, optional). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</description></item>
    /// <item><term>instructions</term><description>Instructions (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// <item><term>dueDate</term><description>Due date (date, optional). Example: <c>2026-01-15</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The redirected movement, with the new step.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement, department, or assignee with that id exists.</response>
    /// <response code="409">The destination department is inactive, or the movement is already closed or canceled.</response>
    [HttpPost("{movementId:guid}/redirect")]
    [EndpointSummary("Redirects the document to another department without closing the movement.")]
    [EndpointDescription("Redirects the document to another department without closing the movement. Requires the DOCUMENT_EDIT permission. Responses: 200 (The redirected movement, with the new step.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement, department, or assignee with that id exists.), 409 (The destination department is inactive, or the movement is already closed or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Redirect(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] RedirectMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RedirectMovementCommand(
                movementId,
                userId,
                request.NewDestinationDepartmentId,
                request.AssignedToUserId,
                request.Instructions,
                request.DueDate),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Closes the movement and releases the document.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The closed movement.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The movement is already closed or canceled.</response>
    [HttpPost("{movementId:guid}/close")]
    [EndpointSummary("Closes the movement and releases the document.")]
    [EndpointDescription("Closes the movement and releases the document. Requires the DOCUMENT_EDIT permission. Responses: 200 (The closed movement.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The movement is already closed or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] CloseMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CloseMovementCommand(movementId, userId, request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Cancels the movement with a required reason.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>Duplicate entry; replaced by a corrected copy.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The canceled movement.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The movement is already closed or canceled.</response>
    [HttpPost("{movementId:guid}/cancel")]
    [EndpointSummary("Cancels the movement with a required reason.")]
    [EndpointDescription("Cancels the movement with a required reason. Requires the DOCUMENT_EDIT permission. Responses: 200 (The canceled movement.), 400 (The reason is missing.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The movement is already closed or canceled.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [FromBody] CancelMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CancelMovementCommand(movementId, userId, request.Reason), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists every movement of a document using the planned document-scoped route.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's movements, newest first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet("/api/documents/{documentId:guid}/movements")]
    [EndpointSummary("Lists every movement of a document using the planned document-scoped route.")]
    [EndpointDescription("Lists every movement of a document, newest first, with its steps and events. This is the planned alias of GET /api/movements/by-document/{documentId}. Requires the DOCUMENT_VIEW permission. Responses: 200 (The document's movements, newest first.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ListByDocumentScoped(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
        => ListByDocument(documentId, cancellationToken);

    /// <summary>Lists a department's inbox using the planned department-scoped route.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department inbox.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the department is outside the caller's scope.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("/api/departments/{departmentId:guid}/inbox")]
    [EndpointSummary("Lists a department's inbox using the planned department-scoped route.")]
    [EndpointDescription("Lists the documents waiting in a department's inbox. This is the planned alias of GET /api/movements/inbox. The department must be inside the caller's organizational scope. Requires the DOCUMENT_VIEW permission. Responses: 200 (The department inbox.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the department is outside the caller's scope.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementQueueItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> InboxScoped(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        CancellationToken cancellationToken)
        => Inbox(departmentId, cancellationToken);

    /// <summary>Lists a department's outbox using the planned department-scoped route.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department outbox.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the department is outside the caller's scope.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("/api/departments/{departmentId:guid}/outbox")]
    [EndpointSummary("Lists a department's outbox using the planned department-scoped route.")]
    [EndpointDescription("Lists the documents a department sent out and that are still elsewhere. This is the planned alias of GET /api/movements/outbox. The department must be inside the caller's organizational scope. Requires the DOCUMENT_VIEW permission. Responses: 200 (The department outbox.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the department is outside the caller's scope.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<MovementQueueItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> OutboxScoped(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        CancellationToken cancellationToken)
        => Outbox(departmentId, cancellationToken);

    /// <summary>Receives a named step when it is the movement's current step.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="stepId">Movement step unique identifier (GUID, route). Example: <c>9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>تم الاستلام</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The recorded receipt.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not current or awaiting receipt, or the department register entry is missing.</response>
    [HttpPost("{movementId:guid}/steps/{stepId:guid}/receive")]
    [EndpointSummary("Receives a named step when it is the movement's current step.")]
    [EndpointDescription("Receives a named step when it is the movement's current step. The destination department must first record its own register number. Requires the DOCUMENT_EDIT permission. Responses: 200 (The recorded receipt.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step is not current or awaiting receipt, or the department register entry is missing.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementStepReceiptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReceiveStep(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [Description("Movement step unique identifier (GUID, route). Example: 9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640.")] Guid stepId,
        [FromBody] ReceiveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ReceiveDocumentCommand(movementId, userId, request.Notes, stepId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Completes a named step when it is the movement's current step.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="stepId">Movement step unique identifier (GUID, route). Example: <c>9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>notes</term><description>Outcome notes (string, optional). Example: <c>انتهى</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The completed movement.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not current, was not received, or the movement is terminal.</response>
    [HttpPost("{movementId:guid}/steps/{stepId:guid}/complete")]
    [EndpointSummary("Completes a named step when it is the movement's current step.")]
    [EndpointDescription("Completes a named step when it is the movement's current step. Only a received step can be completed. Requires the DOCUMENT_EDIT permission. Responses: 200 (The completed movement.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step is not current, was not received, or the movement is terminal.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteStep(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [Description("Movement step unique identifier (GUID, route). Example: 9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640.")] Guid stepId,
        [FromBody] CompleteMovementStepRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CompleteMovementStepCommand(movementId, userId, request.Notes, stepId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Rejects a named step and routes it to the Central Diwan.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="stepId">Movement step unique identifier (GUID, route). Example: <c>9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>غير مطابق</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The rejected movement, routed to the Central Diwan when needed.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not current or rejectable, or the Central Diwan is not configured.</response>
    [HttpPost("{movementId:guid}/steps/{stepId:guid}/reject")]
    [EndpointSummary("Rejects a named step and routes it to the Central Diwan.")]
    [EndpointDescription("Rejects a named step when it is the movement's current step. The rejection is recorded, and a new step back to the configured Central Diwan is opened unless the rejecting department is the Central Diwan itself. Requires the DOCUMENT_EDIT permission. Responses: 200 (The rejected movement, routed to the Central Diwan when needed.), 400 (The reason is missing.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step is not current or rejectable, or the Central Diwan is not configured.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectStep(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [Description("Movement step unique identifier (GUID, route). Example: 9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640.")] Guid stepId,
        [FromBody] RejectMovementStepRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RejectMovementStepCommand(movementId, userId, request.Reason, stepId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns a named step to the sending department.</summary>
    /// <param name="movementId">Movement unique identifier (GUID, route). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="stepId">Movement step unique identifier (GUID, route). Example: <c>9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>reason</term><description>Reason (string, required). Example: <c>يحتاج تصحيح</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The returned movement.</response>
    /// <response code="400">The reason is missing.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_EDIT permission.</response>
    /// <response code="404">No movement with that id exists.</response>
    /// <response code="409">The step is not current, not received, or the movement is terminal.</response>
    [HttpPost("{movementId:guid}/steps/{stepId:guid}/return")]
    [EndpointSummary("Returns a named step to the sending department.")]
    [EndpointDescription("Returns a named step to the sending department when it is the movement's current step. Only a received step can be returned, and the reason is mandatory. Requires the DOCUMENT_EDIT permission. Responses: 200 (The returned movement.), 400 (The reason is missing.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_EDIT permission.), 404 (No movement with that id exists.), 409 (The step is not current, not received, or the movement is terminal.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnStep(
        [Description("Movement unique identifier (GUID, route). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid movementId,
        [Description("Movement step unique identifier (GUID, route). Example: 9e2b7c41-6d55-4f08-a3c9-8b7d1e52a640.")] Guid stepId,
        [FromBody] ReturnMovementToSenderRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ReturnMovementToSenderCommand(movementId, userId, request.Reason, stepId),
            cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
