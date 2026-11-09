using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class PersonRepository : IPersonRepository
{
    private const int StatusKind = 1;
    private const int MovementKind = 2;
    private const int NumberKind = 3;
    private const int ArchiveKind = 4;
    private const int PersonKind = 5;

    private readonly DiwanDbContext _context;

    public PersonRepository(DiwanDbContext context) => _context = context;

    public Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
        _context.Persons.FirstOrDefaultAsync(person => person.PersonId == personId, cancellationToken);

    public Task<bool> ExistsByNationalNumberAsync(
        string nationalNumber,
        Guid? excludingPersonId = null,
        CancellationToken cancellationToken = default) =>
        _context.Persons.AnyAsync(
            person => person.NationalNumber == nationalNumber && person.PersonId != excludingPersonId,
            cancellationToken);

    public void Add(Person person) => _context.Persons.Add(person);

    public async Task<IReadOnlyList<PersonSuggestionRow>> SuggestAsync(
        IReadOnlyList<string> tokens,
        int take,
        CancellationToken cancellationToken = default) =>
        await WhereNameHasAll(_context.Persons.AsNoTracking(), tokens)
            .OrderBy(person => person.NormalizedName)
            .ThenBy(person => person.CreatedAt)
            .Take(take)
            .Select(person => new PersonSuggestionRow(
                person.PersonId,
                person.DisplayName,
                person.BirthDate,
                person.NationalNumber,
                person.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<bool> IsVisibleAsync(
        Guid personId,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default) =>
        ViewableDocumentIds(personId, access).AnyAsync(cancellationToken);

    public async Task<PagedList<PersonListRow>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default)
    {
        var persons = _context.Persons.AsNoTracking();

        if (page.NormalizedSearch is { } search)
        {
            persons = search.All(char.IsAsciiDigit)
                ? persons.Where(person => person.NationalNumber != null && person.NationalNumber.Contains(search))
                : WhereNameHasAll(persons, PersonNameNormalizer.Tokens(search));
        }

        // Grouped once over the visible links; a person with no visible document drops out of the join.
        var statistics = _context.DocumentPersons.AsNoTracking()
            .Join(
                _context.Documents.AsNoTracking().WhereViewableBy(_context, access),
                link => link.DocumentId,
                document => document.DocumentId,
                (link, document) => new { link.PersonId, Activity = document.UpdatedAt ?? document.CreatedAt })
            .GroupBy(row => row.PersonId)
            .Select(group => new
            {
                PersonId = group.Key,
                DocumentCount = group.Count(),
                LastActivityAt = group.Max(row => row.Activity),
            });

        var query = persons.Join(
            statistics,
            person => person.PersonId,
            statistic => statistic.PersonId,
            (person, statistic) => new PersonListProjection
            {
                PersonId = person.PersonId,
                DisplayName = person.DisplayName,
                NormalizedName = person.NormalizedName,
                NationalNumber = person.NationalNumber,
                DocumentCount = statistic.DocumentCount,
                LastActivityAt = statistic.LastActivityAt,
            });

        query = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("name", true) => query.OrderByDescending(row => row.NormalizedName),
            ("name", false) => query.OrderBy(row => row.NormalizedName),
            ("documentcount", true) => query.OrderByDescending(row => row.DocumentCount).ThenBy(row => row.NormalizedName),
            ("documentcount", false) => query.OrderBy(row => row.DocumentCount).ThenBy(row => row.NormalizedName),
            (_, true) => query.OrderByDescending(row => row.LastActivityAt).ThenBy(row => row.NormalizedName),
            _ => query.OrderBy(row => row.LastActivityAt).ThenBy(row => row.NormalizedName),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(row => new PersonListRow(row.PersonId, row.DisplayName, row.NationalNumber, row.DocumentCount, row.LastActivityAt))
            .ToListAsync(cancellationToken);

        return new PagedList<PersonListRow>(items, totalCount);
    }

    public async Task<IReadOnlyList<DocumentPersonRow>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentPersons.AsNoTracking()
            .Where(link => link.DocumentId == documentId)
            .Join(
                _context.Persons.AsNoTracking(),
                link => link.PersonId,
                person => person.PersonId,
                (link, person) => new { Link = link, person.DisplayName })
            .OrderBy(row => row.Link.CreatedAt)
            .Select(row => new DocumentPersonRow(
                row.Link.DocumentPersonId,
                row.Link.PersonId,
                row.DisplayName,
                row.Link.RoleId,
                row.Link.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<PagedList<PersonDocumentRow>> ListDocumentsAsync(
        Guid personId,
        PageRequest page,
        DocumentViewAccess access,
        int? roleId = null,
        int? statusId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentPersons.AsNoTracking()
            .Where(link => link.PersonId == personId)
            .Join(
                _context.Documents.AsNoTracking().WhereViewableBy(_context, access),
                link => link.DocumentId,
                document => document.DocumentId,
                (link, document) => new { Link = link, Document = document });

        if (roleId is { } role)
            query = query.Where(row => row.Link.RoleId == role);

        if (statusId is { } status)
            query = query.Where(row => row.Document.CurrentStatusId == status);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(row =>
                EF.Functions.Like(row.Document.DisplayName, pattern) ||
                (row.Document.OfficialNumberText != null && EF.Functions.Like(row.Document.OfficialNumberText, pattern)));
        }

        query = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(row => row.Document.DisplayName),
            ("title", false) => query.OrderBy(row => row.Document.DisplayName),
            ("status", true) => query.OrderByDescending(row => row.Document.CurrentStatusId),
            ("status", false) => query.OrderBy(row => row.Document.CurrentStatusId),
            (_, true) => query.OrderByDescending(row => row.Link.CreatedAt),
            _ => query.OrderBy(row => row.Link.CreatedAt),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pageRows = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        var documentIds = pageRows.Select(row => row.Document.DocumentId).ToList();

        var locations = await LoadActiveLocationsAsync(documentIds, cancellationToken);
        var archiveLocations = await LoadArchiveLocationsAsync(documentIds, cancellationToken);
        var lastActions = await LoadLastActionsAsync(documentIds, cancellationToken);

        var items = pageRows.Select(row =>
        {
            var document = row.Document;
            var location = locations.GetValueOrDefault(document.DocumentId);
            var archiveLocationId = document.CurrentStatusId == DocumentStatusType.ArchivedId
                ? archiveLocations.GetValueOrDefault(document.DocumentId)
                : null;
            var lastAction = lastActions.GetValueOrDefault(document.DocumentId)
                ?? new LastAction(PersonTimelineKind.Status, document.CreatedAt, document.CreatedByUserId, document.CurrentStatusId, null);

            return new PersonDocumentRow(
                document.DocumentId,
                document.DisplayName,
                document.DocumentTypeId,
                document.OfficialNumberText,
                document.CurrentStatusId,
                document.OwningDepartmentId,
                row.Link.DocumentPersonId,
                row.Link.RoleId,
                row.Link.CreatedAt,
                // An archived document is on a shelf, not in a department.
                archiveLocationId is null ? location?.DepartmentId ?? document.OwningDepartmentId : null,
                archiveLocationId,
                location?.MovementStatusId,
                lastAction.Kind,
                lastAction.OccurredAt,
                lastAction.ActorUserId,
                lastAction.StatusId,
                lastAction.MovementEventTypeId);
        }).ToList();

        return new PagedList<PersonDocumentRow>(items, totalCount);
    }

    public async Task<PagedList<PersonTimelineRow>> ListTimelineAsync(
        Guid personId,
        PageRequest page,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default)
    {
        var documentIds = ViewableDocumentIds(personId, access);

        // Only keys are merged in SQL, which keeps the union free of per-source column differences;
        // the page's details are read afterwards from each source.
        var statuses = _context.DocumentStatusHistories.AsNoTracking()
            .Where(history => documentIds.Contains(history.DocumentId)
                && history.ToStatusId != DocumentStatusType.ArchivedId
                && (history.FromStatusId == null || history.FromStatusId != DocumentStatusType.ArchivedId))
            .Select(history => new TimelineKey { Kind = StatusKind, SourceId = history.HistoryId, OccurredAt = history.ChangedAt });

        var movements = _context.DocumentMovementEvents.AsNoTracking()
            .Join(_context.DocumentMovements.AsNoTracking(), e => e.MovementId, movement => movement.MovementId, (e, movement) => new { e, movement.DocumentId })
            .Where(row => documentIds.Contains(row.DocumentId))
            .Select(row => new TimelineKey { Kind = MovementKind, SourceId = row.e.EventId, OccurredAt = row.e.OccurredAt });

        var numbers = _context.DocumentNumberHistories.AsNoTracking()
            .Join(_context.DocumentNumbers.AsNoTracking(), history => history.DocumentNumberId, number => number.DocumentNumberId, (history, number) => new { history, number.DocumentId })
            .Where(row => documentIds.Contains(row.DocumentId))
            .Select(row => new TimelineKey { Kind = NumberKind, SourceId = row.history.HistoryId, OccurredAt = row.history.OccurredAt });

        var archives = _context.DocumentArchiveEvents.AsNoTracking()
            .Where(archive => documentIds.Contains(archive.DocumentId))
            .Select(archive => new TimelineKey { Kind = ArchiveKind, SourceId = archive.ArchiveEventId, OccurredAt = archive.OccurredAt });

        var links = _context.DocumentPersons.AsNoTracking()
            .Where(link => link.PersonId == personId && documentIds.Contains(link.DocumentId))
            .Select(link => new TimelineKey { Kind = PersonKind, SourceId = link.DocumentPersonId, OccurredAt = link.CreatedAt });

        var merged = statuses.Concat(movements).Concat(numbers).Concat(archives).Concat(links);

        var totalCount = await merged.CountAsync(cancellationToken);
        var keys = await merged
            .OrderByDescending(key => key.OccurredAt)
            .ThenBy(key => key.Kind)
            .ThenBy(key => key.SourceId)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        var rows = await LoadTimelineRowsAsync(keys, cancellationToken);
        var ordered = keys.Select(key => rows[key.SourceId]).ToList();
        return new PagedList<PersonTimelineRow>(ordered, totalCount);
    }

    private IQueryable<Guid> ViewableDocumentIds(Guid personId, DocumentViewAccess access)
    {
        var linked = _context.DocumentPersons.AsNoTracking()
            .Where(link => link.PersonId == personId)
            .Select(link => link.DocumentId);

        return _context.Documents.AsNoTracking()
            .WhereViewableBy(_context, access)
            .Where(document => linked.Contains(document.DocumentId))
            .Select(document => document.DocumentId);
    }

    private static IQueryable<Person> WhereNameHasAll(IQueryable<Person> persons, IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
            persons = persons.Where(person => person.NormalizedName.Contains(token));

        return persons;
    }

    /// <summary>The department each document is at through its active movement, if it has one.</summary>
    private async Task<Dictionary<Guid, ActiveLocation>> LoadActiveLocationsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await _context.DocumentMovements.AsNoTracking()
            .Where(movement => documentIds.Contains(movement.DocumentId)
                && movement.StatusId != DocumentMovement.StatusClosed
                && movement.StatusId != DocumentMovement.StatusCanceled)
            .Join(
                _context.DocumentMovementSteps.AsNoTracking(),
                movement => new { movement.MovementId, StepNo = movement.CurrentStepNo },
                step => new { step.MovementId, step.StepNo },
                (movement, step) => new { movement.DocumentId, movement.StatusId, step.StepStatusId, step.FromDepartmentId, step.ToDepartmentId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.DocumentId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var row = group.First();

                    // Dispatched steps sit with the receiver; pending or bounced ones with the sender.
                    var atReceiver = row.StepStatusId is MovementStepStatus.SentId or MovementStepStatus.ReceivedId or MovementStepStatus.CompletedId;
                    return new ActiveLocation(atReceiver ? row.ToDepartmentId : row.FromDepartmentId, row.StatusId);
                });
    }

    private async Task<Dictionary<Guid, int?>> LoadArchiveLocationsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await _context.DocumentArchiveEvents.AsNoTracking()
            .Where(archive => documentIds.Contains(archive.DocumentId) && archive.ArchiveLocationId != null)
            .Select(archive => new { archive.DocumentId, archive.ArchiveLocationId, archive.OccurredAt })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.DocumentId)
            .ToDictionary(group => group.Key, group => group.MaxBy(row => row.OccurredAt)!.ArchiveLocationId);
    }

    private async Task<Dictionary<Guid, LastAction>> LoadLastActionsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken)
    {
        var statusRows = await _context.DocumentStatusHistories.AsNoTracking()
            .Where(history => documentIds.Contains(history.DocumentId))
            .Select(history => new { history.DocumentId, history.ChangedAt, history.ChangedByUserId, history.ToStatusId })
            .ToListAsync(cancellationToken);

        var movementRows = await _context.DocumentMovementEvents.AsNoTracking()
            .Join(_context.DocumentMovements.AsNoTracking(), e => e.MovementId, movement => movement.MovementId, (e, movement) => new { movement.DocumentId, e.OccurredAt, e.ActorUserId, e.EventTypeId })
            .Where(row => documentIds.Contains(row.DocumentId))
            .ToListAsync(cancellationToken);

        var candidates = statusRows
            .Select(row => (row.DocumentId, Action: new LastAction(PersonTimelineKind.Status, row.ChangedAt, row.ChangedByUserId, row.ToStatusId, null)))
            .Concat(movementRows.Select(row => (row.DocumentId, Action: new LastAction(PersonTimelineKind.Movement, row.OccurredAt, row.ActorUserId, null, row.EventTypeId))));

        // On a tie the movement event says more than the status change it caused.
        return candidates
            .GroupBy(candidate => candidate.DocumentId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => candidate.Action.OccurredAt)
                    .ThenBy(candidate => candidate.Action.Kind == PersonTimelineKind.Movement ? 0 : 1)
                    .First().Action);
    }

    private async Task<Dictionary<Guid, PersonTimelineRow>> LoadTimelineRowsAsync(
        IReadOnlyList<TimelineKey> keys,
        CancellationToken cancellationToken)
    {
        List<Guid> IdsOf(int kind) => keys.Where(key => key.Kind == kind).Select(key => key.SourceId).ToList();

        var statusIds = IdsOf(StatusKind);
        var statuses = await _context.DocumentStatusHistories.AsNoTracking()
            .Where(history => statusIds.Contains(history.HistoryId))
            .ToListAsync(cancellationToken);

        var movementIds = IdsOf(MovementKind);
        var movementEvents = await _context.DocumentMovementEvents.AsNoTracking()
            .Where(e => movementIds.Contains(e.EventId))
            .Join(_context.DocumentMovements.AsNoTracking(), e => e.MovementId, movement => movement.MovementId, (e, movement) => new { Event = e, movement.DocumentId })
            .ToListAsync(cancellationToken);

        var eventMovementIds = movementEvents.Select(row => row.Event.MovementId).Distinct().ToList();
        var steps = (await _context.DocumentMovementSteps.AsNoTracking()
                .Where(step => eventMovementIds.Contains(step.MovementId))
                .Select(step => new { step.MovementId, step.StepNo, step.FromDepartmentId, step.ToDepartmentId })
                .ToListAsync(cancellationToken))
            .ToDictionary(step => (step.MovementId, step.StepNo));

        var numberIds = IdsOf(NumberKind);
        var numberEvents = await _context.DocumentNumberHistories.AsNoTracking()
            .Where(history => numberIds.Contains(history.HistoryId))
            .Join(_context.DocumentNumbers.AsNoTracking(), history => history.DocumentNumberId, number => number.DocumentNumberId, (history, number) => new { History = history, number.DocumentId })
            .ToListAsync(cancellationToken);

        var archiveIds = IdsOf(ArchiveKind);
        var archives = await _context.DocumentArchiveEvents.AsNoTracking()
            .Where(archive => archiveIds.Contains(archive.ArchiveEventId))
            .ToListAsync(cancellationToken);

        var linkIds = IdsOf(PersonKind);
        var links = await _context.DocumentPersons.AsNoTracking()
            .Where(link => linkIds.Contains(link.DocumentPersonId))
            .ToListAsync(cancellationToken);

        var documentIds = statuses.Select(row => row.DocumentId)
            .Concat(movementEvents.Select(row => row.DocumentId))
            .Concat(numberEvents.Select(row => row.DocumentId))
            .Concat(archives.Select(row => row.DocumentId))
            .Concat(links.Select(row => row.DocumentId))
            .Distinct()
            .ToList();

        var titles = await _context.Documents.AsNoTracking()
            .Where(document => documentIds.Contains(document.DocumentId))
            .ToDictionaryAsync(document => document.DocumentId, document => document.DisplayName, cancellationToken);

        var rows = new Dictionary<Guid, PersonTimelineRow>();

        foreach (var history in statuses)
        {
            rows[history.HistoryId] = new PersonTimelineRow(
                PersonTimelineKind.Status, history.ChangedAt, history.DocumentId, titles[history.DocumentId], history.ChangedByUserId,
                FromStatusId: history.FromStatusId,
                ToStatusId: history.ToStatusId);
        }

        foreach (var row in movementEvents)
        {
            steps.TryGetValue((row.Event.MovementId, row.Event.StepNo), out var step);
            rows[row.Event.EventId] = new PersonTimelineRow(
                PersonTimelineKind.Movement, row.Event.OccurredAt, row.DocumentId, titles[row.DocumentId], row.Event.ActorUserId,
                MovementId: row.Event.MovementId,
                MovementEventTypeId: row.Event.EventTypeId,
                FromDepartmentId: step?.FromDepartmentId,
                ToDepartmentId: step?.ToDepartmentId,
                Notes: row.Event.Notes);
        }

        foreach (var row in numberEvents)
        {
            rows[row.History.HistoryId] = new PersonTimelineRow(
                PersonTimelineKind.Number, row.History.OccurredAt, row.DocumentId, titles[row.DocumentId], row.History.ActorUserId,
                NumberEventTypeId: row.History.EventTypeId);
        }

        foreach (var archive in archives)
        {
            rows[archive.ArchiveEventId] = new PersonTimelineRow(
                PersonTimelineKind.Archive, archive.OccurredAt, archive.DocumentId, titles[archive.DocumentId], archive.ActorUserId,
                ArchiveEventTypeId: archive.EventTypeId,
                ArchiveLocationId: archive.ArchiveLocationId,
                Notes: archive.Reason);
        }

        foreach (var link in links)
        {
            rows[link.DocumentPersonId] = new PersonTimelineRow(
                PersonTimelineKind.Person, link.CreatedAt, link.DocumentId, titles[link.DocumentId], link.CreatedByUserId,
                RoleId: link.RoleId);
        }

        return rows;
    }

    private sealed record ActiveLocation(Guid DepartmentId, int MovementStatusId);

    private sealed record LastAction(string Kind, DateTime OccurredAt, Guid ActorUserId, int? StatusId, int? MovementEventTypeId);

    private sealed class TimelineKey
    {
        public int Kind { get; init; }
        public Guid SourceId { get; init; }
        public DateTime OccurredAt { get; init; }
    }

    private sealed class PersonListProjection
    {
        public Guid PersonId { get; init; }
        public string DisplayName { get; init; } = null!;
        public string NormalizedName { get; init; } = null!;
        public string? NationalNumber { get; init; }
        public int DocumentCount { get; init; }
        public DateTime LastActivityAt { get; init; }
    }
}
