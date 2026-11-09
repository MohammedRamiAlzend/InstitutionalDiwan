using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Persons;

/// <summary>Up to ten people whose name holds every word of <paramref name="Query"/>.</summary>
public sealed record SuggestPersonsQuery(string? Query) : IQuery<IReadOnlyList<PersonSuggestionResponse>>;

/// <summary>A page of the people named on documents the caller may view.</summary>
public sealed record ListPersonsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<PagedResult<PersonListItemResponse>>;

public sealed record GetPersonByIdQuery(Guid PersonId) : IQuery<PersonResponse>;

/// <summary>A page of the person's documents the caller may view, each with where it stands now.</summary>
public sealed record ListPersonDocumentsQuery(
    Guid PersonId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    int? RoleId = null,
    int? StatusId = null) : IQuery<PagedResult<PersonDocumentRow>>;

/// <summary>A page of what happened to the person's documents the caller may view, newest first.</summary>
public sealed record GetPersonTimelineQuery(
    Guid PersonId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<PersonTimelineRow>>;

/// <summary>The people named on one document.</summary>
public sealed record ListDocumentPersonsQuery(Guid DocumentId) : IQuery<IReadOnlyList<DocumentPersonResponse>>;

/// <summary>Builds the caller's document-visibility rule and answers whether a person falls inside it.</summary>
public sealed class PersonAccess
{
    private readonly IPersonRepository _personRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public PersonAccess(
        IPersonRepository personRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _personRepository = personRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<DocumentViewAccess> CurrentAsync(CancellationToken cancellationToken) => new(
        _userContext.UserId,
        await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken),
        _clock.UtcNow);

    /// <summary>The caller's access when the person is visible to them; a person outside it reads as not found.</summary>
    public async Task<Result<DocumentViewAccess>> RequireVisibleAsync(Guid personId, CancellationToken cancellationToken)
    {
        var access = await CurrentAsync(cancellationToken);
        return await _personRepository.IsVisibleAsync(personId, access, cancellationToken)
            ? Result.Success(access)
            : Result.Failure<DocumentViewAccess>(DomainErrors.Persons.NotFound);
    }
}

internal sealed class SuggestPersonsQueryHandler : IQueryHandler<SuggestPersonsQuery, IReadOnlyList<PersonSuggestionResponse>>
{
    private const int MinimumQueryLength = 2;
    private const int MaxSuggestions = 10;

    private readonly IPersonRepository _personRepository;

    public SuggestPersonsQueryHandler(IPersonRepository personRepository) => _personRepository = personRepository;

    public async Task<Result<IReadOnlyList<PersonSuggestionResponse>>> Handle(
        SuggestPersonsQuery request,
        CancellationToken cancellationToken)
    {
        if ((PersonNameNormalizer.Normalize(request.Query)?.Length ?? 0) < MinimumQueryLength)
            return Result.Success<IReadOnlyList<PersonSuggestionResponse>>([]);

        var rows = await _personRepository.SuggestAsync(
            PersonNameNormalizer.Tokens(request.Query), MaxSuggestions, cancellationToken);

        return Result.Success<IReadOnlyList<PersonSuggestionResponse>>(rows.Select(PersonSuggestionResponse.From).ToList());
    }
}

internal sealed class ListPersonsQueryHandler : IQueryHandler<ListPersonsQuery, PagedResult<PersonListItemResponse>>
{
    private readonly IPersonRepository _personRepository;
    private readonly PersonAccess _personAccess;

    public ListPersonsQueryHandler(IPersonRepository personRepository, PersonAccess personAccess)
    {
        _personRepository = personRepository;
        _personAccess = personAccess;
    }

    public async Task<Result<PagedResult<PersonListItemResponse>>> Handle(
        ListPersonsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var rows = await _personRepository.ListPagedAsync(
            page, await _personAccess.CurrentAsync(cancellationToken), cancellationToken);

        return Result.Success(new PagedResult<PersonListItemResponse>(
            rows.Items.Select(PersonListItemResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}

internal sealed class GetPersonByIdQueryHandler : IQueryHandler<GetPersonByIdQuery, PersonResponse>
{
    private readonly IPersonRepository _personRepository;
    private readonly PersonAccess _personAccess;

    public GetPersonByIdQueryHandler(IPersonRepository personRepository, PersonAccess personAccess)
    {
        _personRepository = personRepository;
        _personAccess = personAccess;
    }

    public async Task<Result<PersonResponse>> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        var access = await _personAccess.RequireVisibleAsync(request.PersonId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<PersonResponse>(access.Error);

        var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken);
        return person is null
            ? Result.Failure<PersonResponse>(DomainErrors.Persons.NotFound)
            : Result.Success(PersonResponse.From(person));
    }
}

internal sealed class ListPersonDocumentsQueryHandler : IQueryHandler<ListPersonDocumentsQuery, PagedResult<PersonDocumentRow>>
{
    private readonly IPersonRepository _personRepository;
    private readonly PersonAccess _personAccess;

    public ListPersonDocumentsQueryHandler(IPersonRepository personRepository, PersonAccess personAccess)
    {
        _personRepository = personRepository;
        _personAccess = personAccess;
    }

    public async Task<Result<PagedResult<PersonDocumentRow>>> Handle(
        ListPersonDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await _personAccess.RequireVisibleAsync(request.PersonId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<PagedResult<PersonDocumentRow>>(access.Error);

        var page = new PageRequest(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDescending);
        var rows = await _personRepository.ListDocumentsAsync(
            request.PersonId, page, access.Value, request.RoleId, request.StatusId, cancellationToken);

        return Result.Success(new PagedResult<PersonDocumentRow>(
            rows.Items, page.NormalizedPage, page.NormalizedPageSize, rows.TotalCount));
    }
}

internal sealed class GetPersonTimelineQueryHandler : IQueryHandler<GetPersonTimelineQuery, PagedResult<PersonTimelineRow>>
{
    private readonly IPersonRepository _personRepository;
    private readonly PersonAccess _personAccess;

    public GetPersonTimelineQueryHandler(IPersonRepository personRepository, PersonAccess personAccess)
    {
        _personRepository = personRepository;
        _personAccess = personAccess;
    }

    public async Task<Result<PagedResult<PersonTimelineRow>>> Handle(
        GetPersonTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var access = await _personAccess.RequireVisibleAsync(request.PersonId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<PagedResult<PersonTimelineRow>>(access.Error);

        var page = new PageRequest(request.Page, request.PageSize);
        var rows = await _personRepository.ListTimelineAsync(request.PersonId, page, access.Value, cancellationToken);

        return Result.Success(new PagedResult<PersonTimelineRow>(
            rows.Items, page.NormalizedPage, page.NormalizedPageSize, rows.TotalCount));
    }
}

internal sealed class ListDocumentPersonsQueryHandler : IQueryHandler<ListDocumentPersonsQuery, IReadOnlyList<DocumentPersonResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public ListDocumentPersonsQueryHandler(
        IDocumentRepository documentRepository,
        IPersonRepository personRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _personRepository = personRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<IReadOnlyList<DocumentPersonResponse>>> Handle(
        ListDocumentPersonsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<DocumentPersonResponse>>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(request.DocumentId, AccessLevel.ViewId, cancellationToken))
            return Result.Failure<IReadOnlyList<DocumentPersonResponse>>(DomainErrors.Correspondence.DocumentAccessDenied);

        var rows = await _personRepository.ListByDocumentAsync(request.DocumentId, cancellationToken);
        return Result.Success<IReadOnlyList<DocumentPersonResponse>>(rows.Select(DocumentPersonResponse.From).ToList());
    }
}
