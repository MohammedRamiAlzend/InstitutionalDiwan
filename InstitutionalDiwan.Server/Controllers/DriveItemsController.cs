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
/// Module 11 (Drive) — folders and files: access-filtered browse, read, breadcrumb, children,
/// create folder, rename, move (cycle-safe), and personal stars. Per-item access is checked in the
/// handlers through the Drive access evaluator.
/// </summary>
[ApiController]
[Route("api/drive/items")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveItemsController : ControllerBase
{
    private readonly ISender _sender;

    public DriveItemsController(ISender sender) => _sender = sender;

    /// <summary>Browses items the caller can reach, paged and filterable.</summary>
    /// <param name="page">Page number (integer, query, optional, default 1). Example: <c>1</c>.</param>
    /// <param name="pageSize">Page size (integer, query, optional, 1–100, default 20). Example: <c>20</c>.</param>
    /// <param name="spaceId">Restrict to one space (GUID, query, optional). Example: <c>3f2a9c14-7d58-4e62-9b10-2c84f6a7d901</c>.</param>
    /// <param name="parentId">Restrict to one parent folder; null is the space root (GUID, query, optional). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="search">Name substring filter (string, query, optional). Example: <c>تقرير</c>.</param>
    /// <param name="kind">Item kind filter (integer, query, optional; 1 = folder, 2 = file). Example: <c>2</c>.</param>
    /// <param name="ownerUserId">Restrict to one owner (GUID, query, optional). Example: <c>1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84</c>.</param>
    /// <param name="trashed">Include only trashed items (boolean, query, optional, default false). Example: <c>false</c>.</param>
    /// <param name="starred">Include only the caller's starred items (boolean, query, optional, default false). Example: <c>true</c>.</param>
    /// <param name="modifiedFrom">Modified-at lower bound (UTC datetime, query, optional). Example: <c>2026-11-01T00:00:00Z</c>.</param>
    /// <param name="modifiedTo">Modified-at upper bound (UTC datetime, query, optional). Example: <c>2026-11-30T23:59:59Z</c>.</param>
    /// <param name="sortBy">Sort field (string, query, optional; name/modifiedAt/size). Example: <c>name</c>.</param>
    /// <param name="sortDescending">Sort direction (boolean, query, optional, default false). Example: <c>false</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">A page of items the caller can reach.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_USE.</response>
    [HttpGet]
    [EndpointSummary("Browses items the caller can reach, paged and filterable.")]
    [EndpointDescription("Returns a server-paged, access-filtered list of Drive items (folders and files). Filters: space, parent, name search, kind, owner, trashed, starred, modified window; sortable by name/modifiedAt/size. Access is enforced in SQL. Requires the DRIVE_USE permission. Responses: 200 (A page of items the caller can reach.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_USE.)")]
    [ProducesResponseType<PagedResult<DriveItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Browse(
        [Description("Page number (integer, query, optional, default 1). Example: 1.")] int page = 1,
        [Description("Page size (integer, query, optional, 1–100, default 20). Example: 20.")] int pageSize = 20,
        [Description("Restrict to one space (GUID, query, optional). Example: 3f2a9c14-7d58-4e62-9b10-2c84f6a7d901.")] Guid? spaceId = null,
        [Description("Restrict to one parent folder; null is the space root (GUID, query, optional). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid? parentId = null,
        [Description("Name substring filter (string, query, optional). Example: تقرير.")] string? search = null,
        [Description("Item kind filter (integer, query, optional; 1 = folder, 2 = file). Example: 2.")] int? kind = null,
        [Description("Restrict to one owner (GUID, query, optional). Example: 1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84.")] Guid? ownerUserId = null,
        [Description("Include only trashed items (boolean, query, optional, default false). Example: false.")] bool trashed = false,
        [Description("Include only the caller's starred items (boolean, query, optional, default false). Example: true.")] bool starred = false,
        [Description("Modified-at lower bound (UTC datetime, query, optional). Example: 2026-11-01T00:00:00Z.")] DateTime? modifiedFrom = null,
        [Description("Modified-at upper bound (UTC datetime, query, optional). Example: 2026-11-30T23:59:59Z.")] DateTime? modifiedTo = null,
        [Description("Sort field (string, query, optional; name/modifiedAt/size). Example: name.")] string? sortBy = null,
        [Description("Sort direction (boolean, query, optional, default false). Example: false.")] bool sortDescending = false,
        CancellationToken cancellationToken = default)
        => (await _sender.Send(new BrowseDriveItemsQuery(
            page, pageSize, spaceId, parentId, search, kind, ownerUserId, trashed, starred,
            modifiedFrom, modifiedTo, sortBy, sortDescending), cancellationToken)).ToActionResult();

    /// <summary>Reads one item the caller can view.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The item.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot view the item.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpGet("{itemId:guid}")]
    [EndpointSummary("Reads one item the caller can view.")]
    [EndpointDescription("Returns a single Drive item (folder or file) after a Viewer access check. Requires the DRIVE_USE permission. Responses: 200 (The item.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the item.), 404 (No item with that id exists.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveItemQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Returns the item's ancestor chain, root first, ending with the item itself.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The breadcrumb nodes, root first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot view the item.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpGet("{itemId:guid}/breadcrumb")]
    [EndpointSummary("Returns the item's ancestor chain, root first, ending with the item itself.")]
    [EndpointDescription("Builds the breadcrumb path from the space root down to the item. Requires the DRIVE_USE permission and Viewer access to the item. Responses: 200 (The breadcrumb nodes, root first.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the item.), 404 (No item with that id exists.)")]
    [ProducesResponseType<IReadOnlyList<DriveBreadcrumbNode>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Breadcrumb(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveBreadcrumbQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Lists the direct children of a folder (or the space root).</summary>
    /// <param name="spaceId">Space unique identifier (GUID, query, required). Example: <c>3f2a9c14-7d58-4e62-9b10-2c84f6a7d901</c>.</param>
    /// <param name="parentId">Parent folder id; null lists the space root (GUID, query, optional). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The direct children, folders first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot view the parent (or root).</response>
    [HttpGet("children")]
    [EndpointSummary("Lists the direct children of a folder (or the space root).")]
    [EndpointDescription("Returns the immediate children of a folder, or of the space root when parentId is omitted. Requires the DRIVE_USE permission and Viewer access to the parent (root needs space owner/ADMIN). Responses: 200 (The direct children, folders first.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the parent or root.)")]
    [ProducesResponseType<IReadOnlyList<DriveItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Children(
        [Description("Space unique identifier (GUID, query, required). Example: 3f2a9c14-7d58-4e62-9b10-2c84f6a7d901.")] Guid spaceId,
        [Description("Parent folder id; null lists the space root (GUID, query, optional). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid? parentId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveChildrenQuery(spaceId, parentId), cancellationToken)).ToActionResult();

    /// <summary>Creates a folder in a space.</summary>
    /// <param name="request">The space, optional parent folder, and folder name.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created folder.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot create at the destination.</response>
    /// <response code="404">The space or parent folder does not exist.</response>
    /// <response code="409">The name is taken, or the depth limit is reached.</response>
    [HttpPost("folders")]
    [EndpointSummary("Creates a folder in a space.")]
    [EndpointDescription("Creates a folder at the space root (owner/ADMIN) or inside a parent folder (Editor+). Enforces sibling-name uniqueness and the depth limit. Requires the DRIVE_USE permission. Responses: 200 (The created folder.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot create at the destination.), 404 (The space or parent folder does not exist.), 409 (The name is taken, or the depth limit is reached.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFolder(
        [FromBody] CreateDriveFolderRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new CreateDriveFolderCommand(request.SpaceId, request.ParentId, request.Name), cancellationToken)).ToActionResult();

    /// <summary>Renames an item (Editor+).</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="request">The new name.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The renamed item.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Editor access.</response>
    /// <response code="404">No item with that id exists.</response>
    /// <response code="409">The name is taken among siblings.</response>
    [HttpPut("{itemId:guid}/rename")]
    [EndpointSummary("Renames an item (Editor+).")]
    [EndpointDescription("Renames a folder or file. Requires Editor access and a name not already taken among its siblings. Requires the DRIVE_USE permission. Responses: 200 (The renamed item.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Editor access.), 404 (No item with that id exists.), 409 (The name is taken among siblings.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rename(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [FromBody] RenameDriveItemRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RenameDriveItemCommand(itemId, request.Name), cancellationToken)).ToActionResult();

    /// <summary>Moves an item to another folder or the space root (cycle-safe).</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="request">The new parent folder id, or null for the space root.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The moved item.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot move the item.</response>
    /// <response code="404">The item or destination does not exist.</response>
    /// <response code="409">The move creates a cycle, exceeds the depth limit, or the name is taken.</response>
    [HttpPut("{itemId:guid}/move")]
    [EndpointSummary("Moves an item to another folder or the space root (cycle-safe).")]
    [EndpointDescription("Moves a folder or file. Moving into its own descendant is rejected (cycle); the depth limit and sibling-name uniqueness are enforced. Cross-space moves require Manager on the item. Requires the DRIVE_USE permission. Responses: 200 (The moved item.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot move the item.), 404 (The item or destination does not exist.), 409 (The move creates a cycle, exceeds the depth limit, or the name is taken.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Move(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [FromBody] MoveDriveItemRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new MoveDriveItemCommand(itemId, request.NewParentId), cancellationToken)).ToActionResult();

    /// <summary>Stars an item for the caller (Viewer+, idempotent).</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The item is starred.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot view the item.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpPost("{itemId:guid}/star")]
    [EndpointSummary("Stars an item for the caller (Viewer+, idempotent).")]
    [EndpointDescription("Adds the item to the caller's personal favorites. Idempotent. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The item is starred.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the item.), 404 (No item with that id exists.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Star(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new StarDriveItemCommand(itemId), cancellationToken)).ToActionResult();

    /// <summary>Removes the caller's star from an item (idempotent).</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The item is unstarred.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller cannot view the item.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpDelete("{itemId:guid}/star")]
    [EndpointSummary("Removes the caller's star from an item (idempotent).")]
    [EndpointDescription("Removes the item from the caller's personal favorites. Idempotent. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The item is unstarred.), 401 (The token is missing, expired, or invalid.), 403 (The caller cannot view the item.), 404 (No item with that id exists.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unstar(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new UnstarDriveItemCommand(itemId), cancellationToken)).ToActionResult();
}