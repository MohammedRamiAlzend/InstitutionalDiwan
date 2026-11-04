using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Notifications;
using InstitutionalDiwan.Server.Extensions;
using InstitutionalDiwan.Server.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 10: the caller's own in-app notifications. Every route is scoped to the authenticated user,
/// so no permission code is needed and another user's row answers 404 exactly like a missing one.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>Lists the caller's notifications, newest first.</summary>
    /// <param name="unreadOnly">Return only unread notifications (boolean, query, optional, default <c>false</c>). Example: <c>true</c>.</param>
    /// <param name="typeId">Filter by notification type id (integer, query, optional). Example: <c>3</c>.</param>
    /// <param name="page">One-based page number (integer, query, optional, default <c>1</c>). Example: <c>1</c>.</param>
    /// <param name="pageSize">Rows per page, at most 100 (integer, query, optional, default <c>20</c>). Example: <c>20</c>.</param>
    /// <param name="search">Text to find in the Arabic title or body (string, query, optional). Example: <c>وثيقة</c>.</param>
    /// <param name="sortBy">Sort column: deliveredAt (default, newest first), priority, type, isRead (string, query, optional). Example: <c>priority</c>.</param>
    /// <param name="sortDescending">Sort descending instead of ascending when <c>sortBy</c> is given (boolean, query, optional, default <c>false</c>). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of the caller's notifications.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet]
    [EndpointSummary("Lists the caller's notifications, newest first.")]
    [EndpointDescription("Lists the caller's own in-app notifications, newest first by default; search matches the Arabic title or body and sortBy can be deliveredAt, priority, type or isRead. Dismissed notifications are never listed. Any authenticated user may call it; no permission code is required. Responses: 200 (One page of the caller's notifications.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<PagedResult<NotificationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery, Description("Return only unread notifications (boolean, optional, default false). Example: true.")] bool unreadOnly = false,
        [FromQuery, Description("Filter by notification type id (integer, optional). Example: 3.")] int? typeId = null,
        [FromQuery, Description("One-based page number (integer, optional, default 1). Example: 1.")] int page = 1,
        [FromQuery, Description("Rows per page, at most 100 (integer, optional, default 20). Example: 20.")] int pageSize = 20,
        [FromQuery, Description("Text to find in the Arabic title or body (string, optional). Example: وثيقة.")] string? search = null,
        [FromQuery, Description("Sort column: deliveredAt (default, newest first), priority, type, isRead (string, optional). Example: priority.")] string? sortBy = null,
        [FromQuery, Description("Sort descending instead of ascending when sortBy is given (boolean, optional, default false). Example: true.")] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ListNotificationsQuery(userId, unreadOnly, typeId, page, pageSize, search, sortBy, sortDescending), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Counts the caller's unread notifications.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The unread count.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet("unread-count")]
    [SkipActionAudit]
    [EndpointSummary("Counts the caller's unread notifications.")]
    [EndpointDescription("Counts the caller's unread, non-dismissed notifications in total and per notification type (byType). Meant for the bell badge, so this route is not recorded in the action audit trail. Any authenticated user may call it. Responses: 200 (The unread count.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new GetUnreadCountQuery(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Marks one of the caller's notifications as read.</summary>
    /// <param name="recipientId">Notification recipient row identifier from the list (GUID, route). Example: <c>5c1e0b7a-3d24-4f68-9a0e-8b7d6c5e4f31</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The notification is read (repeating the call changes nothing).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">The caller has no notification with that id.</response>
    [HttpPost("{recipientId:guid}/read")]
    [EndpointSummary("Marks one of the caller's notifications as read.")]
    [EndpointDescription("Marks one of the caller's own notifications as read. The call is idempotent. A row that belongs to another user answers 404 exactly like a missing one. Responses: 200 (The notification is read.), 401 (The token is missing, expired, or invalid.), 404 (The caller has no notification with that id.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        [Description("Notification recipient row identifier from the list (GUID, route). Example: 5c1e0b7a-3d24-4f68-9a0e-8b7d6c5e4f31.")] Guid recipientId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new MarkNotificationReadCommand(recipientId, userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Marks every unread notification of the caller as read.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">How many notifications changed.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpPost("read-all")]
    [EndpointSummary("Marks every unread notification of the caller as read.")]
    [EndpointDescription("Marks every unread, non-dismissed notification of the caller as read and returns how many changed. Responses: 200 (How many notifications changed.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<MarkedReadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new MarkAllNotificationsReadCommand(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Hides one of the caller's notifications.</summary>
    /// <param name="recipientId">Notification recipient row identifier from the list (GUID, route). Example: <c>5c1e0b7a-3d24-4f68-9a0e-8b7d6c5e4f31</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The notification is hidden (repeating the call changes nothing).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">The caller has no notification with that id.</response>
    [HttpPost("{recipientId:guid}/dismiss")]
    [EndpointSummary("Hides one of the caller's notifications.")]
    [EndpointDescription("Dismisses one of the caller's own notifications so it no longer appears in the list or the unread count. The call is idempotent. A row that belongs to another user answers 404 exactly like a missing one. Responses: 200 (The notification is hidden.), 401 (The token is missing, expired, or invalid.), 404 (The caller has no notification with that id.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dismiss(
        [Description("Notification recipient row identifier from the list (GUID, route). Example: 5c1e0b7a-3d24-4f68-9a0e-8b7d6c5e4f31.")] Guid recipientId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new DismissNotificationCommand(recipientId, userId), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
