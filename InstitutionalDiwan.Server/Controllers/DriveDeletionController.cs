using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Drive;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — approval-gated deletion: request deletion of an item, list requests,
/// approve/reject (owner's department head or ADMIN), cancel (requester only), and direct delete
/// where the caller is privileged enough to skip the approval flow.
/// </summary>
[ApiController]
[Route("api/drive")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveDeletionController : ControllerBase
{
    private readonly ISender _sender;

    public DriveDeletionController(ISender sender) => _sender = sender;

    /// <summary>Requests approval-gated deletion of an item.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="request">The reason for the deletion request.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created deletion request (pending).</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller may not request deletion of this item.</response>
    /// <response code="404">No item with that id exists.</response>
    /// <response code="409">A pending request already exists for the item.</response>
    [HttpPost("items/{itemId:guid}/deletion-requests")]
    [EndpointSummary("Requests approval-gated deletion of an item.")]
    [EndpointDescription("Creates a Pending deletion request routed to the owner's department head (or ADMIN when none) and notifies the reviewer. Applies even to personal files. Requires the DRIVE_USE permission. Responses: 200 (The created deletion request.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller may not request deletion of this item.), 404 (No item with that id exists.), 409 (A pending request already exists for the item.)")]
    [ProducesResponseType<DriveDeletionRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestDeletion(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [FromBody] RequestDriveItemDeletionRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RequestDriveItemDeletionCommand(itemId, request.Reason), cancellationToken)).ToActionResult();

    /// <summary>Lists deletion requests visible to the caller.</summary>
    /// <param name="page">1-based page number (integer, query, optional; default 1). Example: <c>1</c>.</param>
    /// <param name="pageSize">Page size (integer, query, optional; default 20). Example: <c>20</c>.</param>
    /// <param name="statusId">Filter by request status id (integer, query, optional). Example: <c>1</c>.</param>
    /// <param name="mineOnly">When true, only the caller's own requests (boolean, query, optional; default false). Example: <c>false</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">A page of deletion requests.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet("deletion-requests")]
    [EndpointSummary("Lists deletion requests visible to the caller.")]
    [EndpointDescription("Returns the caller's own requests when mineOnly=true; otherwise ADMIN sees all and a department head sees the requests they may review (filtered in SQL). Requires the DRIVE_USE permission. Responses: 200 (A page of deletion requests.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<PagedResult<DriveDeletionRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [Description("1-based page number (integer, query, optional; default 1). Example: 1.")] int page = 1,
        [Description("Page size (integer, query, optional; default 20). Example: 20.")] int pageSize = 20,
        [Description("Filter by request status id (integer, query, optional). Example: 1.")] int? statusId = null,
        [Description("When true, only the caller's own requests (boolean, query, optional; default false). Example: false.")] bool mineOnly = false,
        CancellationToken cancellationToken = default)
        => (await _sender.Send(new ListDriveDeletionRequestsQuery(page, pageSize, statusId, mineOnly), cancellationToken)).ToActionResult();

    /// <summary>Approves a deletion request (owner's department head or ADMIN).</summary>
    /// <param name="requestId">Deletion request unique identifier (GUID, route). Example: <c>3b7d9e21-5c84-4f16-a073-8e19d4f62a57</c>.</param>
    /// <param name="request">Optional reviewer note.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The approved request; the item is trashed.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller may not review this request.</response>
    /// <response code="404">No request with that id exists.</response>
    /// <response code="409">The request is not pending.</response>
    [HttpPost("deletion-requests/{requestId:guid}/approve")]
    [EndpointSummary("Approves a deletion request (owner's department head or ADMIN).")]
    [EndpointDescription("Approves a Pending request, trashes the item, and notifies the requester. Requires the DRIVE_USE permission and review authority. Responses: 200 (The approved request.), 401 (The token is missing, expired, or invalid.), 403 (The caller may not review this request.), 404 (No request with that id exists.), 409 (The request is not pending.)")]
    [ProducesResponseType<DriveDeletionRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        [Description("Deletion request unique identifier (GUID, route). Example: 3b7d9e21-5c84-4f16-a073-8e19d4f62a57.")] Guid requestId,
        [FromBody] ReviewDriveDeletionRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new ApproveDriveDeletionCommand(requestId, request.Note), cancellationToken)).ToActionResult();

    /// <summary>Rejects a deletion request (owner's department head or ADMIN).</summary>
    /// <param name="requestId">Deletion request unique identifier (GUID, route). Example: <c>3b7d9e21-5c84-4f16-a073-8e19d4f62a57</c>.</param>
    /// <param name="request">Optional reviewer note.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The rejected request; the item is kept.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller may not review this request.</response>
    /// <response code="404">No request with that id exists.</response>
    /// <response code="409">The request is not pending.</response>
    [HttpPost("deletion-requests/{requestId:guid}/reject")]
    [EndpointSummary("Rejects a deletion request (owner's department head or ADMIN).")]
    [EndpointDescription("Rejects a Pending request, keeps the item, and notifies the requester. Requires the DRIVE_USE permission and review authority. Responses: 200 (The rejected request.), 401 (The token is missing, expired, or invalid.), 403 (The caller may not review this request.), 404 (No request with that id exists.), 409 (The request is not pending.)")]
    [ProducesResponseType<DriveDeletionRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        [Description("Deletion request unique identifier (GUID, route). Example: 3b7d9e21-5c84-4f16-a073-8e19d4f62a57.")] Guid requestId,
        [FromBody] ReviewDriveDeletionRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RejectDriveDeletionCommand(requestId, request.Note), cancellationToken)).ToActionResult();

    /// <summary>Cancels a deletion request (requester only).</summary>
    /// <param name="requestId">Deletion request unique identifier (GUID, route). Example: <c>3b7d9e21-5c84-4f16-a073-8e19d4f62a57</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The cancelled request.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">Only the requester may cancel.</response>
    /// <response code="404">No request with that id exists.</response>
    /// <response code="409">The request is not pending.</response>
    [HttpPost("deletion-requests/{requestId:guid}/cancel")]
    [EndpointSummary("Cancels a deletion request (requester only).")]
    [EndpointDescription("Cancels the caller's own Pending deletion request. Requires the DRIVE_USE permission. Responses: 200 (The cancelled request.), 401 (The token is missing, expired, or invalid.), 403 (Only the requester may cancel.), 404 (No request with that id exists.), 409 (The request is not pending.)")]
    [ProducesResponseType<DriveDeletionRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [Description("Deletion request unique identifier (GUID, route). Example: 3b7d9e21-5c84-4f16-a073-8e19d4f62a57.")] Guid requestId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new CancelDriveDeletionCommand(requestId), cancellationToken)).ToActionResult();

    /// <summary>Directly trashes an item where the caller is privileged enough to skip approval.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The trashed item.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller must use the approval flow instead.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpDelete("items/{itemId:guid}")]
    [EndpointSummary("Directly trashes an item where the caller is privileged enough to skip approval.")]
    [EndpointDescription("Trashes an item directly when the caller is the owner's department head or ADMIN (the privileged path); otherwise the approval flow must be used. Requires the DRIVE_USE permission. Responses: 200 (The trashed item.), 401 (The token is missing, expired, or invalid.), 403 (The caller must use the approval flow instead.), 404 (No item with that id exists.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDirectly(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new DeleteDriveItemDirectlyCommand(itemId), cancellationToken)).ToActionResult();
}