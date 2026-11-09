using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>Holds persons in memory for handlers that only add or fetch them; read models are covered in SQL.</summary>
internal sealed class FakePersonRepository : IPersonRepository
{
    public List<Person> Persons { get; } = new();

    public Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default)
        => Task.FromResult(Persons.FirstOrDefault(person => person.PersonId == personId));

    public Task<bool> ExistsByNationalNumberAsync(
        string nationalNumber,
        Guid? excludingPersonId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Persons.Any(person => person.NationalNumber == nationalNumber && person.PersonId != excludingPersonId));

    public void Add(Person person) => Persons.Add(person);

    public Task<IReadOnlyList<PersonSuggestionRow>> SuggestAsync(IReadOnlyList<string> tokens, int take, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> IsVisibleAsync(Guid personId, DocumentViewAccess access, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedList<PersonListRow>> ListPagedAsync(PageRequest page, DocumentViewAccess access, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<DocumentPersonRow>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedList<PersonDocumentRow>> ListDocumentsAsync(
        Guid personId,
        PageRequest page,
        DocumentViewAccess access,
        int? roleId = null,
        int? statusId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedList<PersonTimelineRow>> ListTimelineAsync(Guid personId, PageRequest page, DocumentViewAccess access, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
