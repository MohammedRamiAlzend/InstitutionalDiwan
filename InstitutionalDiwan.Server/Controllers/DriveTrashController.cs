using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — the 30-day trash: list trashed items the caller may reach, restore an item
/// to its original location, and permanently purge an item (releasing quota and deleting objects).
/// </summary>
[ApiController]
[Route("api/drive/trash")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveTrashController : ControllerBase
{
    private readonly ISender _sender;

    public DriveTrashController(ISender sender) => _sender = sender;

    /// <summary>Lists trashed items the caller may reach.</summary>
    /// <param name="page">1-based page number (integer, query, optional; default 1). Example: <c>1</c>.</param>
    /// <param name="pageSize">Page size (integer, query, optional; default 20). Example: <c>20</c>.</param>
    /// <param name="spaceId">Filter by space (GUID, query, optional). Example: <c>4e2a7f93-1d68-4c05-b934-6f02e8a71d49</c>.</param>
    /// <param name="search">Filter by name substring (string, query, optional). Example: <c>تقرير</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">A page of trashed items.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet]
    [EndpointSummary("Lists trashed items the caller may reach.")]
    [EndpointDescription("Returns trashed items reachable by the caller (owner, space owner, ADMIN, or the owner's department head), newest trashed first. Requires the DRIVE_USE permission. Responses: 200 (A page of trashed items.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<PagedResult<DriveItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [Description("1-based page number (integer, query, optional; default 1). Example: 1.")] int page = 1,
        [Description("Page size (integer, query, optional; default 20). Example: 20.")] int pageSize = 20,
        [Description("Filter by space (GUID, query, optional). Example: 4e2a7f93-1d68-4c05-b934-6f02e8a71d49.")] Guid? spaceId = null,
        [Description("Filter by name substring (string, query, optional). Example: تقرير.")] string? search = null,
        CancellationToken cancellationToken = default)
        => (await _sender.Send(new ListDriveTrashQuery(page, pageSize, spaceId, search), cancellationToken)).ToActionResult();

    /// <summary>Restores a trashed item to its original location.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The restored item.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller may not restore this item.</response>
    /// <response code="404">No trashed item with that id exists.</response>
    /// <response code="409">The item is not trashed, or its parent is gone.</response>
    [HttpPost("{itemId:guid}/restore")]
    [EndpointSummary("Restores a trashed item to its original location.")]
    [EndpointDescription("Clears TrashedAt on the item (and its subtree) and restores it under its original parent. Requires the DRIVE_USE permission and restore authority. Responses: 200 (The restored item.), 401 (The token is missing, expired, or invalid.), 403 (The caller may not restore this item.), 404 (No trashed item with that id exists.), 409 (The item is not trashed, or its parent is gone.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RestoreDriveItemCommand(itemId), cancellationToken)).ToActionResult();

    /// <summary>Permanently purges a trashed item, releasing quota and deleting stored objects.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The item is permanently purged.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller may not purge this item.</response>
    /// <response code="404">No trashed item with that id exists.</response>
    /// <response code="409">The item is not trashed.</response>
    [HttpDelete("{itemId:guid}")]
    [EndpointSummary("Permanently purges a trashed item, releasing quota and deleting stored objects.")]
    [EndpointDescription("Hard-deletes a trashed item and its subtree, deletes every stored version object, and releases the space usage. Requires the DRIVE_USE permission and purge authority. Responses: 200 (The item is permanently purged.), 401 (The token is missing, expired, or invalid.), 403 (The caller may not purge this item.), 404 (No trashed item with that id exists.), 409 (The item is not trashed.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Purge(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new PurgeDriveItemCommand(itemId), cancellationToken)).ToActionResult();
}