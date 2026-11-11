using InstitutionalDiwan.Domain.Entities.Persons;

namespace InstitutionalDiwan.Domain.Repositories;

/// <summary>A match offered while typing a name: only enough to tell two people apart.</summary>
public sealed record PersonSuggestionRow(
    Guid PersonId,
    string DisplayName,
    DateOnly? BirthDate,
    string? NationalNumber,
    DateTime CreatedAt);

/// <summary>A row of the persons list; the counters cover only documents the caller may view.</summary>
public sealed record PersonListRow(
    Guid PersonId,
    string DisplayName,
    string? NationalNumber,
    int DocumentCount,
    DateTime LastActivityAt);

/// <summary>A person named on one document.</summary>
public sealed record DocumentPersonRow(
    Guid DocumentPersonId,
    Guid PersonId,
    string DisplayName,
    int RoleId,
    DateTime CreatedAt);

/// <summary>
/// Another unarchived document of the same type that shares a person with the document being read.
/// A suggestion only; it is not a join.
/// </summary>
public sealed record JoinCandidateRow(
    Guid DocumentId,
    string Title,
    string? OfficialNumberText,
    string PersonName);

/// <summary>One of a person's documents with where it stands now and the last thing done to it.</summary>
public sealed record PersonDocumentRow(
    Guid DocumentId,
    string Title,
    int DocumentTypeId,
    string? OfficialNumberText,
    int StatusId,
    Guid OwningDepartmentId,
    Guid DocumentPersonId,
    int RoleId,
    DateTime LinkedAt,
    Guid? CurrentDepartmentId,
    int? CurrentArchiveLocationId,
    int? ActiveMovementStatusId,
    string LastActionKind,
    DateTime LastActionAt,
    Guid LastActionUserId,
    int? LastStatusId,
    int? LastMovementEventTypeId);

/// <summary>One event of a person's merged timeline. Which optional values are set depends on <see cref="Kind"/>.</summary>
public sealed record PersonTimelineRow(
    string Kind,
    DateTime OccurredAt,
    Guid DocumentId,
    string DocumentTitle,
    Guid ActorUserId,
    int? FromStatusId = null,
    int? ToStatusId = null,
    Guid? MovementId = null,
    int? MovementEventTypeId = null,
    Guid? FromDepartmentId = null,
    Guid? ToDepartmentId = null,
    int? NumberEventTypeId = null,
    int? ArchiveEventTypeId = null,
    int? ArchiveLocationId = null,
    int? RoleId = null,
    string? Notes = null);

public static class PersonTimelineKind
{
    public const string Status = "STATUS";
    public const string Movement = "MOVEMENT";
    public const string Number = "NUMBER";
    public const string Archive = "ARCHIVE";
    public const string Person = "PERSON";
}

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNationalNumberAsync(
        string nationalNumber,
        Guid? excludingPersonId = null,
        CancellationToken cancellationToken = default);

    void Add(Person person);

    /// <summary>
    /// People whose name holds every token. Deliberately not limited by document access, so the same
    /// person is not registered again from another department; callers expose only this short row.
    /// </summary>
    Task<IReadOnlyList<PersonSuggestionRow>> SuggestAsync(
        IReadOnlyList<string> tokens,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>True when the person is named on at least one document the caller may view.</summary>
    Task<bool> IsVisibleAsync(Guid personId, DocumentViewAccess access, CancellationToken cancellationToken = default);

    /// <summary>
    /// A page of the people named on documents the caller may view. <see cref="PageRequest.Search"/>
    /// matches the national number when it is all digits and the name otherwise. Sort keys:
    /// <c>name</c>, <c>documentCount</c>, <c>lastActivity</c> (the default).
    /// </summary>
    Task<PagedList<PersonListRow>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentPersonRow>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unarchived, uncanceled documents of <paramref name="documentTypeId"/> that share a person with
    /// <paramref name="documentId"/> and that <paramref name="access"/> can view. The current document
    /// is left out. One row per other document.
    /// </summary>
    Task<IReadOnlyList<JoinCandidateRow>> ListJoinCandidatesAsync(
        Guid documentId,
        int documentTypeId,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default);

    /// <summary>A page of the person's documents the caller may view. Sort keys: <c>title</c>, <c>status</c>, <c>linkedAt</c> (the default).</summary>
    Task<PagedList<PersonDocumentRow>> ListDocumentsAsync(
        Guid personId,
        PageRequest page,
        DocumentViewAccess access,
        int? roleId = null,
        int? statusId = null,
        CancellationToken cancellationToken = default);

    /// <summary>A page of what happened to the person's documents the caller may view, newest first.</summary>
    Task<PagedList<PersonTimelineRow>> ListTimelineAsync(
        Guid personId,
        PageRequest page,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default);
}
