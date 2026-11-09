using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Server.Authorization;
using InstitutionalDiwan.Server.Contracts.Persons;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// People named on documents who are not system users. Reads require <c>PERSON_VIEW</c> and cover only
/// documents the caller may view; a person with no such document reads as not found.
/// </summary>
[ApiController]
[Route("api/persons")]
[Produces("application/json")]
public sealed class PersonsController : ControllerBase
{
    private readonly ISender _sender;

    public PersonsController(ISender sender) => _sender = sender;

    /// <summary>Suggests existing people while a name is being typed.</summary>
    /// <param name="q">Part of the person's name, two characters at least (string, optional). Example: <c>أحمد</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">Up to ten matches.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token has neither DOCUMENT_CREATE nor DOCUMENT_EDIT.</response>
    [HttpGet("suggestions")]
    [Authorize(Policy = PersonPolicies.Suggest)]
    [EndpointSummary("Suggests existing people while a name is being typed.")]
    [EndpointDescription("Suggests existing people while a name is being typed. Searches every person so nobody is registered twice from another department, and returns only the name, birth year, last four digits of the national number, and date added. Requires DOCUMENT_CREATE or DOCUMENT_EDIT. Responses: 200 (Up to ten matches.), 401 (The token is missing, expired, or invalid.), 403 (The token has neither DOCUMENT_CREATE nor DOCUMENT_EDIT.)")]
    [ProducesResponseType<IReadOnlyList<PersonSuggestionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Suggest(
        [Description("Part of the person's name, two characters at least (string, optional). Example: أحمد.")] [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SuggestPersonsQuery(q), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists a page of the people named on documents the caller may view.</summary>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Name words, or digits of the national number (string, optional). Example: <c>أحمد</c>.</param>
    /// <param name="sortBy">Sort key: <c>name</c>, <c>documentCount</c>, or <c>lastActivity</c> (string, optional). Example: <c>name</c>.</param>
    /// <param name="sortDescending">Sort descending when true (boolean, optional). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of people with the paging counters.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the PERSON_VIEW permission.</response>
    [HttpGet]
    [Authorize(Policy = Permissions.PersonView)]
    [EndpointSummary("Lists a page of the people named on documents the caller may view.")]
    [EndpointDescription("Lists a page of the people named on documents the caller may view, with how many of those documents each one has and when one of them last changed. Requires the PERSON_VIEW permission. Responses: 200 (One page of people with the paging counters.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the PERSON_VIEW permission.)")]
    [ProducesResponseType<PagedResult<PersonListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Name words, or digits of the national number (string, optional). Example: أحمد.")] [FromQuery] string? search = null,
        [Description("Sort key: name, documentCount, lastActivity (string, optional). Example: name.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListPersonsQuery(page, pageSize, search, sortBy, sortDescending), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a person's full record.</summary>
    /// <param name="personId">Person unique identifier (GUID, route). Example: <c>5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The person.</response>
    /// <response code="403">The token lacks the PERSON_VIEW permission.</response>
    /// <response code="404">No such person, or none of their documents is visible to the caller.</response>
    [HttpGet("{personId:guid}")]
    [Authorize(Policy = Permissions.PersonView)]
    [EndpointSummary("Fetches a person's full record.")]
    [EndpointDescription("Fetches a person's full record. Requires the PERSON_VIEW permission and at least one of the person's documents to be visible to the caller. Responses: 200 (The person.), 403 (The token lacks the PERSON_VIEW permission.), 404 (No such person, or none of their documents is visible to the caller.)")]
    [ProducesResponseType<PersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Description("Person unique identifier (GUID, route). Example: 5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22.")] Guid personId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPersonByIdQuery(personId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Replaces a person's recorded details.</summary>
    /// <param name="personId">Person unique identifier (GUID, route). Example: <c>5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22</c>.</param>
    /// <param name="request">
    /// The request payload; every field is optional and one left out is cleared:
    /// <list type="bullet">
    /// <item><term>fullName</term><description>Full name (string). Example: <c>أحمد محمد علي</c>.</description></item>
    /// <item><term>firstName</term><description>First name (string). Example: <c>أحمد</c>.</description></item>
    /// <item><term>lastName</term><description>Family name (string). Example: <c>علي</c>.</description></item>
    /// <item><term>fatherName</term><description>Father's name (string). Example: <c>محمد</c>.</description></item>
    /// <item><term>motherName</term><description>Mother's name (string). Example: <c>فاطمة</c>.</description></item>
    /// <item><term>nationalNumber</term><description>National number, unique when present (string). Example: <c>01020304050</c>.</description></item>
    /// <item><term>civilRegistryNumber</term><description>Civil registry number (string). Example: <c>125</c>.</description></item>
    /// <item><term>civilRegistryPlace</term><description>Civil registry office or district (string). Example: <c>الميدان</c>.</description></item>
    /// <item><term>birthPlace</term><description>Place of birth (string). Example: <c>دمشق</c>.</description></item>
    /// <item><term>birthDate</term><description>Date of birth (ISO date). Example: <c>1980-05-14</c>.</description></item>
    /// <item><term>genderId</term><description>Gender id (integer). Example: <c>1</c>.</description></item>
    /// <item><term>nationalityId</term><description>Nationality id (integer). Example: <c>1</c>.</description></item>
    /// <item><term>phone</term><description>Phone number (string). Example: <c>+963944000000</c>.</description></item>
    /// <item><term>address</term><description>Address (string). Example: <c>دمشق، المزة</c>.</description></item>
    /// <item><term>notes</term><description>Notes (string). Example: <c>مقيم خارج البلاد</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated person.</response>
    /// <response code="400">The payload failed validation, or left the person with no name or national number.</response>
    /// <response code="403">The token lacks the PERSON_MANAGE permission.</response>
    /// <response code="404">No such person, or none of their documents is visible to the caller.</response>
    /// <response code="409">Another person already has this national number.</response>
    [HttpPut("{personId:guid}")]
    [EndpointSummary("Replaces a person's recorded details.")]
    [EndpointDescription("Replaces a person's recorded details; a value left out is cleared. Requires the PERSON_VIEW and PERSON_MANAGE permissions and at least one of the person's documents to be visible to the caller. Responses: 200 (The updated person.), 400 (The payload failed validation, or left the person with no name or national number.), 403 (The token lacks the PERSON_MANAGE permission.), 404 (No such person, or none of their documents is visible to the caller.), 409 (Another person already has this national number.)")]
    [Authorize(Policy = Permissions.PersonView)]
    [Authorize(Policy = Permissions.PersonManage)]
    [ProducesResponseType<PersonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [Description("Person unique identifier (GUID, route). Example: 5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22.")] Guid personId,
        [FromBody] UpdatePersonRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new UpdatePersonCommand(personId, userId, request.ToDetails()), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists a page of a person's documents, each with where it stands now.</summary>
    /// <param name="personId">Person unique identifier (GUID, route). Example: <c>5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22</c>.</param>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Title or official number search (string, optional). Example: <c>تبليغ</c>.</param>
    /// <param name="sortBy">Sort key: <c>title</c>, <c>status</c>, or <c>linkedAt</c> (string, optional). Example: <c>title</c>.</param>
    /// <param name="sortDescending">Sort descending when true (boolean, optional). Example: <c>true</c>.</param>
    /// <param name="roleId">Filter by the person's role on the document (integer, optional). Example: <c>1</c>.</param>
    /// <param name="statusId">Filter by document status id (integer, optional). Example: <c>2</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of documents with the paging counters.</response>
    /// <response code="403">The token lacks the PERSON_VIEW permission.</response>
    /// <response code="404">No such person, or none of their documents is visible to the caller.</response>
    [HttpGet("{personId:guid}/documents")]
    [Authorize(Policy = Permissions.PersonView)]
    [EndpointSummary("Lists a page of a person's documents, each with where it stands now.")]
    [EndpointDescription("Lists a page of a person's documents the caller may view: the person's role, the document's status, the department or archive location holding it now, and the last action taken. Requires the PERSON_VIEW permission. Responses: 200 (One page of documents with the paging counters.), 403 (The token lacks the PERSON_VIEW permission.), 404 (No such person, or none of their documents is visible to the caller.)")]
    [ProducesResponseType<PagedResult<PersonDocumentRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListDocuments(
        [Description("Person unique identifier (GUID, route). Example: 5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22.")] Guid personId,
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Title or official number search (string, optional). Example: تبليغ.")] [FromQuery] string? search = null,
        [Description("Sort key: title, status, linkedAt (string, optional). Example: title.")] [FromQuery] string? sortBy = null,
        [Description("Sort descending when true (boolean, optional). Example: true.")] [FromQuery] bool sortDescending = true,
        [Description("Filter by the person's role on the document (integer, optional). Example: 1.")] [FromQuery] int? roleId = null,
        [Description("Filter by document status id (integer, optional). Example: 2.")] [FromQuery] int? statusId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListPersonDocumentsQuery(personId, page, pageSize, search, sortBy, sortDescending, roleId, statusId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists a page of what happened to a person's documents, newest first.</summary>
    /// <param name="personId">Person unique identifier (GUID, route). Example: <c>5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22</c>.</param>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of events with the paging counters.</response>
    /// <response code="403">The token lacks the PERSON_VIEW permission.</response>
    /// <response code="404">No such person, or none of their documents is visible to the caller.</response>
    [HttpGet("{personId:guid}/timeline")]
    [Authorize(Policy = Permissions.PersonView)]
    [EndpointSummary("Lists a page of what happened to a person's documents, newest first.")]
    [EndpointDescription("Merges status changes, movement events, number events, archive events, and the person's own additions across the person's documents the caller may view, newest first. Requires the PERSON_VIEW permission. Responses: 200 (One page of events with the paging counters.), 403 (The token lacks the PERSON_VIEW permission.), 404 (No such person, or none of their documents is visible to the caller.)")]
    [ProducesResponseType<PagedResult<PersonTimelineRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(
        [Description("Person unique identifier (GUID, route). Example: 5b1f7a3e-2c44-4d19-8e6a-0f3c9d7b1a22.")] Guid personId,
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPersonTimelineQuery(personId, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
