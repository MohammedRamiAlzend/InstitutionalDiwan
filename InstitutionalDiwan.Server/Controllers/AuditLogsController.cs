using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Audit;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Audit;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 09 — tamper-evident audit trail. The log is append-only and every row carries the previous
/// row's SHA-256 hash, so mutating or deleting a historical entry breaks the chain and is reported by
/// the integrity checker. Reading the log is itself audited.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Produces("application/json")]
[Authorize]
public sealed class AuditLogsController : ControllerBase
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender) => _sender = sender;

    /// <summary>Lists a filtered page of audit entries and records the search as an audit event.</summary>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>1</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="moduleName">Module name filter (string, optional). Example: <c>DOCUMENTS</c>.</param>
    /// <param name="actionTypeId">Action type id filter (integer, optional; 1 = create, 2 = update, 3 = delete, 4 = view). Example: <c>2</c>.</param>
    /// <param name="entityTypeId">Entity type id filter (integer, optional). Example: <c>1</c>.</param>
    /// <param name="entityId">Entity id filter (string, optional). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="actorUserId">Actor user id filter (GUID, optional). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="actorDepartmentId">Actor department id filter (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="relatedDocumentId">Related document id filter (GUID, optional). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="correlationId">Correlation id filter (string, optional). Example: <c>0HN7G4K2Q8L1M</c>.</param>
    /// <param name="ipAddress">Client IP filter (string, optional). Example: <c>10.0.0.15</c>.</param>
    /// <param name="fromUtc">Occurred-at lower bound (UTC date-time, optional). Example: <c>2026-01-01T00:00:00Z</c>.</param>
    /// <param name="toUtc">Occurred-at upper bound (UTC date-time, optional). Example: <c>2026-12-31T23:59:59Z</c>.</param>
    /// <param name="search">Free-text search over module, entity id, correlation id, and IP (string, optional). Example: <c>DOCUMENT</c>.</param>
    /// <param name="sortBy">Sort key (string, optional): <c>occurredAt</c>, <c>module</c>, <c>action</c>, or <c>entity</c>. Example: <c>occurredAt</c>.</param>
    /// <param name="sortDescending">Sort direction (boolean, optional, default <c>true</c>). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of audit entries plus the paging counters.</response>
    /// <response code="400">The date range is invalid.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the AUDIT_VIEW permission.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet]
    [EndpointSummary("Lists a filtered page of audit entries and records the search as an audit event.")]
    [EndpointDescription("Lists a filtered page of audit entries and appends a self-audit entry for the search. Supports paging, date range, actor/entity filters, and sorting. Requires the AUDIT_VIEW permission. Responses: 200 (One page of audit entries plus the paging counters.), 400 (The date range is invalid.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the AUDIT_VIEW permission.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.AuditView)]
    [ProducesResponseType<PagedResult<AuditLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Page number (integer, optional, 1-based). Example: 1.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Module name filter (string, optional). Example: DOCUMENTS.")] [FromQuery] string? moduleName = null,
        [Description("Action type id filter (integer, optional). Example: 2.")] [FromQuery] int? actionTypeId = null,
        [Description("Entity type id filter (integer, optional). Example: 1.")] [FromQuery] int? entityTypeId = null,
        [Description("Entity id filter (string, optional). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] [FromQuery] string? entityId = null,
        [Description("Actor user id filter (GUID, optional). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] [FromQuery] Guid? actorUserId = null,
        [Description("Actor department id filter (GUID, optional). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] [FromQuery] Guid? actorDepartmentId = null,
        [Description("Related document id filter (GUID, optional). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] [FromQuery] Guid? relatedDocumentId = null,
        [Description("Correlation id filter (string, optional). Example: 0HN7G4K2Q8L1M.")] [FromQuery] string? correlationId = null,
        [Description("Client IP filter (string, optional). Example: 10.0.0.15.")] [FromQuery] string? ipAddress = null,
        [Description("Occurred-at lower bound in UTC (date-time, optional). Example: 2026-01-01T00:00:00Z.")] [FromQuery] DateTime? fromUtc = null,
        [Description("Occurred-at upper bound in UTC (date-time, optional). Example: 2026-12-31T23:59:59Z.")] [FromQuery] DateTime? toUtc = null,
        [Description("Free-text search term (string, optional). Example: DOCUMENT.")] [FromQuery] string? search = null,
        [Description("Sort key (string, optional): occurredAt, module, action, or entity. Example: occurredAt.")] [FromQuery] string? sortBy = null,
        [Description("Sort direction (boolean, optional, default true). Example: true.")] [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new GetAuditLogsQuery(
                userId,
                page,
                pageSize,
                moduleName,
                actionTypeId,
                entityTypeId,
                entityId,
                actorUserId,
                actorDepartmentId,
                relatedDocumentId,
                correlationId,
                ipAddress,
                fromUtc,
                toUtc,
                search,
                sortBy,
                sortDescending),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Seals all audit entries added since the previous checkpoint into a new anchor.</summary>
    /// <remarks>
    /// The chain is verified before the anchor is written, so a broken chain can never be sealed as
    /// valid. If no new entries exist, the request returns 409.
    /// </remarks>
    /// <param name="request">Optional upper bound; omit it to seal through the latest entry.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created checkpoint.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the AUDIT_MANAGE permission.</response>
    /// <response code="404">No audit entries or no user account were found.</response>
    /// <response code="409">There are no new entries since the previous checkpoint, or the chain is broken.</response>
    [HttpPost("checkpoints")]
    [EndpointSummary("Seals all audit entries added since the previous checkpoint into a new anchor.")]
    [EndpointDescription("Verifies the chain, then seals every entry after the previous checkpoint into an immutable anchor. Requires the AUDIT_MANAGE permission. Responses: 200 (The created checkpoint.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the AUDIT_MANAGE permission.), 404 (No audit entries or no user account were found.), 409 (There are no new entries since the previous checkpoint, or the chain is broken.)")]
    [Authorize(Policy = Permissions.AuditManage)]
    [ProducesResponseType<AuditCheckpointResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCheckpoint(
        [FromBody] CreateAuditCheckpointRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateAuditCheckpointCommand(userId, request.ToAuditLogId),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Walks the audit chain, recomputes every SHA-256 hash, and stores the report.</summary>
    /// <remarks>
    /// The default range is the whole chain. A broken chain is not an HTTP error: the command returns
    /// 200 with <c>isValid = false</c> and the first corrupted audit id.
    /// </remarks>
    /// <param name="request">Optional start/end ids; omit both to verify the full chain.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The integrity report, valid or invalid.</response>
    /// <response code="400">The requested range is invalid.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the AUDIT_MANAGE permission.</response>
    /// <response code="404">No entries or no user account were found for the range.</response>
    [HttpPost("integrity-checks")]
    [EndpointSummary("Walks the audit chain, recomputes every SHA-256 hash, and stores the report.")]
    [EndpointDescription("Walks the selected audit range, verifies each previous-hash link and recomputed event hash, and persists the report. Requires the AUDIT_MANAGE permission. Responses: 200 (The integrity report, valid or invalid.), 400 (The requested range is invalid.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the AUDIT_MANAGE permission.), 404 (No entries or no user account were found for the range.)")]
    [Authorize(Policy = Permissions.AuditManage)]
    [ProducesResponseType<AuditIntegrityCheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunIntegrityCheck(
        [FromBody] RunAuditIntegrityCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new RunAuditIntegrityCheckCommand(
                userId,
                request.StartAuditLogId,
                request.EndAuditLogId),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists a page of persisted audit-integrity reports, newest first.</summary>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>1</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of integrity reports.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the AUDIT_VIEW permission.</response>
    [HttpGet("integrity-checks")]
    [EndpointSummary("Lists a page of persisted audit-integrity reports, newest first.")]
    [EndpointDescription("Lists the reports produced by POST /api/audit-logs/integrity-checks. Requires the AUDIT_VIEW permission. Responses: 200 (One page of integrity reports.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the AUDIT_VIEW permission.)")]
    [Authorize(Policy = Permissions.AuditView)]
    [ProducesResponseType<PagedResult<AuditIntegrityCheckResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListIntegrityChecks(
        [Description("Page number (integer, optional, 1-based). Example: 1.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAuditIntegrityReportsQuery(page, pageSize), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
