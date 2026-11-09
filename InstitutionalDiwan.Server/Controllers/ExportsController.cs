using InstitutionalDiwan.Application.Features.Exports;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Payload of <c>POST /api/exports/table</c>: a table exactly as the caller sees it.</summary>
public sealed record ExportTableRequest(
    string Title,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

[ApiController]
[Route("api/exports")]
[Authorize]
public sealed class ExportsController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ISender _sender;

    public ExportsController(ISender sender) => _sender = sender;

    /// <summary>Builds an Excel workbook from a table the caller has on screen.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>title</term><description>Table title (string, required). Example: <c>سجل الوثائق</c>.</description></item>
    /// <item><term>headers</term><description>Column headers (array, required, at most 100). Example: <c>["الوثيقة","الحالة"]</c>.</description></item>
    /// <item><term>rows</term><description>Rows of text, one value per header (array, required, at most 5000). Example: <c>[["سجل أساس · 77/2026","مسجل"]]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The workbook.</response>
    /// <response code="400">The table is malformed or has too many rows.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpPost("table")]
    [EndpointSummary("Builds an Excel workbook from a table the caller has on screen.")]
    [EndpointDescription("Turns the supplied headers and rows into a right-to-left .xlsx workbook. Reads nothing from the database, so any signed-in caller may use it. Responses: 200 (The workbook.), 400 (The table is malformed or has too many rows.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ExcelContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Table([FromBody] ExportTableRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ExportTableQuery(request.Title, request.Headers, request.Rows), cancellationToken);

        return result.IsFailure
            ? result.Error.ToActionResult()
            : File(result.Value.Content, ExcelContentType, result.Value.FileName);
    }
}
