using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The register book of a department: documents written into that department's register, with the
/// department's own number and date and the central Diwan number kept alongside.
/// </summary>
[ApiController]
[Route("api/departments/{departmentId:guid}/register")]
[Produces("application/json")]
[Authorize(Policy = Permissions.NumberingView)]
public sealed class DepartmentRegistersController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentRegistersController(ISender sender) => _sender = sender;

    /// <summary>Lists a department's register entries ordered by register date.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="numberingYear">Numbering year (integer, optional, query parameter). Example: <c>2026</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The register lines.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet]
    [EndpointSummary("Lists a department's register entries ordered by register date.")]
    [EndpointDescription("Lists a department's register entries ordered by register date. The department must be inside the caller's organizational scope. Every line is listed, but the document's title and central number are empty when the caller has no VIEW access to that document. Requires the NUMBERING_VIEW permission. Responses: 200 (The register lines.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.), 404 (No department with that id exists.)")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentRegisterLineResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        [Description("Numbering year (integer, optional, query parameter). Example: 2026.")] [FromQuery] int? numberingYear,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListDepartmentRegisterQuery(departmentId, numberingYear), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists a page of one department's register lines, narrowed and ordered on the server.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="registryId">Only lines written in this department register (GUID, optional). Example: <c>3f1c2a9e-5b7d-4c8e-9a10-2d4b6e8f0a12</c>.</param>
    /// <param name="numberingYear">Only lines of this numbering year (integer, optional). Example: <c>2026</c>.</param>
    /// <param name="statusId">Only lines with this number status id (integer, optional). Example: <c>1</c>.</param>
    /// <param name="dateFrom">Only lines registered on or after this day (date, optional). Example: <c>2026-11-01</c>.</param>
    /// <param name="dateTo">Only lines registered on or before this day (date, optional). Example: <c>2026-11-30</c>.</param>
    /// <param name="schemaVersionId">A record form version of the department; keeps only lines with a visit on it and returns that visit's values (GUID, optional). Example: <c>9b2f6d1a-7c3e-4f58-8a0d-1e5c7b9d3f24</c>.</param>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Free-text search across the register number, the document title, the central number and the text values of the chosen form (string, optional). Example: <c>45</c>.</param>
    /// <param name="sortBy">Sort key: registryNumber, registryDate, document, centralNumber, numberingYear, status, registrationCount, or field:{key} for a field of the chosen form (string, optional). Example: <c>registryDate</c>.</param>
    /// <param name="sortDescending">Sort descending when true (boolean, optional). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of register lines with the paging counters.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.</response>
    /// <response code="404">No department with that id exists, or the form version is not one of its issued forms.</response>
    [HttpGet("entries")]
    [EndpointSummary("Lists a page of one department's register lines, narrowed and ordered on the server.")]
    [EndpointDescription("Lists a page of one department's register lines, newest registration first by default. Search, filters, sorting and paging all run in the database. Passing schemaVersionId limits the page to lines with a visit on that department record form version and returns that visit's values keyed by field key. A line whose document the caller cannot view keeps its number, but its title and central number are empty and never match a search. The department must be inside the caller's organizational scope. Requires the NUMBERING_VIEW permission. Responses: 200 (One page of register lines with the paging counters.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.), 404 (No department with that id exists, or the form version is not one of its issued forms.)")]
    [ProducesResponseType<PagedResult<DepartmentRegisterEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEntries(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        [Description("Only lines written in this department register (GUID, optional). Example: 3f1c2a9e-5b7d-4c8e-9a10-2d4b6e8f0a12.")] [FromQuery] Guid? registryId = null,
        [Description("Only lines of this numbering year (integer, optional). Example: 2026.")] [FromQuery] int? numberingYear = null,
        [Description("Only lines with this number status id (integer, optional). Example: 1.")] [FromQuery] int? statusId = null,
        [Description("Only lines registered on or after this day (date, optional). Example: 2026-11-01.")] [FromQuery] DateTime? dateFrom = null,
        [Description("Only lines registered on or before this day (date, optional). Example: 2026-11-30.")] [FromQuery] DateTime? dateTo = null,
        [Description("A record form version of the department; keeps only lines with a visit on it and returns that visit's values (GUID, optional). Example: 9b2f6d1a-7c3e-4f58-8a0d-1e5c7b9d3f24.")] [FromQuery] Guid? schemaVersionId = null,
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Free-text search across the register number, the document title, the central number and the text values of the chosen form (string, optional). Example: 45.")] [FromQuery] string? search = null,
        [Description("Sort key: registryNumber, registryDate, document, centralNumber, numberingYear, status, registrationCount, or field:{key} for a field of the chosen form (string, optional). Example: registryDate.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListDepartmentRegisterEntriesQuery(
                departmentId,
                registryId,
                numberingYear,
                statusId,
                dateFrom,
                dateTo,
                schemaVersionId,
                page,
                pageSize,
                search,
                sortBy,
                sortDescending),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists the record forms a department has issued, per document type, newest version first.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department's issued record forms; empty when it has none.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet("forms")]
    [EndpointSummary("Lists the record forms a department has issued, per document type, newest version first.")]
    [EndpointDescription("Lists the record forms a department has issued, grouped by document type. Each form carries every published or retired version with its fields, newest first, so a screen can open on the latest version and offer the older ones. Drafts are never listed. The department must be inside the caller's organizational scope. Requires the NUMBERING_VIEW permission. Responses: 200 (The department's issued record forms; empty when it has none.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks NUMBERING_VIEW, or the department is outside the caller's scope.), 404 (No department with that id exists.)")]
    [ProducesResponseType<DepartmentRegisterFormsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForms(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentRegisterFormsQuery(departmentId), cancellationToken);
        return result.ToActionResult();
    }
}
