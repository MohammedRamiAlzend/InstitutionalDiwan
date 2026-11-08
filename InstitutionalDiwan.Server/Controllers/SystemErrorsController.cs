using System.ComponentModel;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Diagnostics;
using InstitutionalDiwan.Server.Contracts.Diagnostics;
using InstitutionalDiwan.Server.Extensions;
using InstitutionalDiwan.Server.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The developer's log of captured errors: unexpected exceptions, refused requests and failures
/// reported by the browser, grouped so a repeated defect is one row.
/// </summary>
[ApiController]
[Route("api/system-errors")]
[Produces("application/json")]
[Authorize]
public sealed class SystemErrorsController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int MaxClientReportBytes = 64 * 1024;

    private readonly ISender _sender;

    public SystemErrorsController(ISender sender) => _sender = sender;

    /// <summary>Lists a page of captured errors.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.SystemErrorsView)]
    [EndpointSummary("Lists a page of captured errors, most recently seen first by default.")]
    [EndpointDescription("Lists a page of captured errors, each grouping every occurrence of the same defect. Requires the SYSTEM_ERRORS_VIEW permission. Responses: 200 (One page of errors with the paging counters.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks SYSTEM_ERRORS_VIEW.)")]
    [ProducesResponseType<PagedResult<SystemErrorSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Page number (integer, optional, 1-based). Example: 1.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Free-text search across title, message, and path (string, optional). Example: NullReference.")] [FromQuery] string? search = null,
        [Description("Sort key: lastSeenAt, firstSeenAt, occurrenceCount, title, status (string, optional). Example: lastSeenAt.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        [Description("Work status: 1 new, 2 in progress, 3 resolved (integer, optional). Example: 1.")] [FromQuery] SystemErrorStatus? status = null,
        [Description("Origin: 1 server, 2 browser (integer, optional). Example: 1.")] [FromQuery] SystemErrorSource? source = null,
        [Description("Kind: 1 server exception, 2 refused request, 3 page crash, 4 script error, 5 network failure (integer, optional). Example: 1.")] [FromQuery] SystemErrorKind? kind = null,
        [Description("Last seen at or after this UTC time (date-time, optional). Example: 2026-01-01T00:00:00Z.")] [FromQuery] DateTime? lastSeenFromUtc = null,
        [Description("Last seen at or before this UTC time (date-time, optional). Example: 2026-12-31T23:59:59Z.")] [FromQuery] DateTime? lastSeenToUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListSystemErrorsQuery(page, pageSize, search, sortBy, sortDescending, status, source, kind, lastSeenFromUtc, lastSeenToUtc),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Exports the captured errors matching the filters to Excel.</summary>
    [HttpGet("export")]
    [Authorize(Policy = Permissions.SystemErrorsView)]
    [EndpointSummary("Exports the captured errors matching the filters to an Excel workbook.")]
    [EndpointDescription("Builds an .xlsx workbook of the errors matching the filters, up to 10000 rows, in the requested order. Requires the SYSTEM_ERRORS_VIEW permission. Responses: 200 (The workbook.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks SYSTEM_ERRORS_VIEW.)")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ExcelContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [Description("Free-text search across title, message, and path (string, optional). Example: NullReference.")] [FromQuery] string? search = null,
        [Description("Sort key: lastSeenAt, firstSeenAt, occurrenceCount, title, status (string, optional). Example: lastSeenAt.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        [Description("Work status: 1 new, 2 in progress, 3 resolved (integer, optional). Example: 1.")] [FromQuery] SystemErrorStatus? status = null,
        [Description("Origin: 1 server, 2 browser (integer, optional). Example: 1.")] [FromQuery] SystemErrorSource? source = null,
        [Description("Kind: 1 server exception, 2 refused request, 3 page crash, 4 script error, 5 network failure (integer, optional). Example: 1.")] [FromQuery] SystemErrorKind? kind = null,
        [Description("Last seen at or after this UTC time (date-time, optional). Example: 2026-01-01T00:00:00Z.")] [FromQuery] DateTime? lastSeenFromUtc = null,
        [Description("Last seen at or before this UTC time (date-time, optional). Example: 2026-12-31T23:59:59Z.")] [FromQuery] DateTime? lastSeenToUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ExportSystemErrorsQuery(search, sortBy, sortDescending, status, source, kind, lastSeenFromUtc, lastSeenToUtc),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToActionResult()
            : File(result.Value.Content, ExcelContentType, result.Value.FileName);
    }

    /// <summary>Returns one captured error with its stack trace.</summary>
    [HttpGet("{systemErrorId:guid}")]
    [Authorize(Policy = Permissions.SystemErrorsView)]
    [EndpointSummary("Returns one captured error with its stack trace.")]
    [EndpointDescription("Returns one captured error with the technical details of its latest occurrence and the developer's notes. Requires the SYSTEM_ERRORS_VIEW permission. Responses: 200 (The error.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks SYSTEM_ERRORS_VIEW.), 404 (No such error.)")]
    [ProducesResponseType<SystemErrorDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [Description("Captured error identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid systemErrorId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSystemErrorQuery(systemErrorId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Sets the work status and notes of a captured error.</summary>
    [HttpPut("{systemErrorId:guid}")]
    [Authorize(Policy = Permissions.SystemErrorsView)]
    [EndpointSummary("Sets the work status and notes of a captured error.")]
    [EndpointDescription("Sets how far a developer got with a captured error. An error seen again after being resolved reopens as new. Requires the SYSTEM_ERRORS_VIEW permission. Responses: 200 (The updated error.), 400 (The status or notes are invalid.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks SYSTEM_ERRORS_VIEW.), 404 (No such error.)")]
    [ProducesResponseType<SystemErrorDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [Description("Captured error identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid systemErrorId,
        [FromBody] UpdateSystemErrorRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new UpdateSystemErrorCommand(systemErrorId, request.Status, request.Notes), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Permanently deletes the errors not seen for a chosen number of days.</summary>
    [HttpPost("purge")]
    [Authorize(Policy = Permissions.SystemErrorsView)]
    [EndpointSummary("Permanently deletes the errors not seen for a chosen number of days.")]
    [EndpointDescription("Permanently deletes every captured error whose last occurrence is older than the given number of days and returns how many were deleted. Requires the SYSTEM_ERRORS_VIEW permission. Responses: 200 (The number of deleted errors.), 400 (The number of days is outside 1-3650.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks SYSTEM_ERRORS_VIEW.)")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Purge(
        [FromBody] PurgeSystemErrorsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new PurgeSystemErrorsCommand(request.OlderThanDays), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Records a failure observed by the signed-in user's browser.</summary>
    [HttpPost("client-reports")]
    [SkipActionAudit]
    [RequestSizeLimit(MaxClientReportBytes)]
    [EndpointSummary("Records a failure observed by the signed-in user's browser.")]
    [EndpointDescription("Records a page crash, script error, or network failure seen in the browser. Open to any signed-in user, because any user's browser can fail, and not written to the action audit. Responses: 200 (The report was accepted.), 400 (The report is invalid.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Report(
        [FromBody] ClientErrorReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ReportClientErrorCommand(
                request.Kind, request.Message, request.Details, request.Path, request.HttpMethod, request.StatusCode,
                Request.Headers.UserAgent.ToString()),
            cancellationToken);
        return result.ToActionResult();
    }
}
