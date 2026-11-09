using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Persons;

/// <summary>
/// One person to name on a document: an existing person by <paramref name="PersonId"/>, or a new one
/// by <paramref name="FullName"/>. A null <paramref name="RoleId"/> takes the catalog's default role.
/// </summary>
public sealed record DocumentPersonInput(Guid? PersonId = null, string? FullName = null, int? RoleId = null);

/// <summary>
/// Names people on a document for the create flows and the add endpoint alike. A typed name always
/// creates a new person: only an explicit <see cref="DocumentPersonInput.PersonId"/> reuses one,
/// because two people can share a name.
/// </summary>
public sealed class DocumentPersonLinker
{
    private readonly IPersonRepository _personRepository;
    private readonly ILookupRepository _lookupRepository;

    public DocumentPersonLinker(IPersonRepository personRepository, ILookupRepository lookupRepository)
    {
        _personRepository = personRepository;
        _lookupRepository = lookupRepository;
    }

    public async Task<Result<IReadOnlyList<DocumentPerson>>> LinkAsync(
        Document document,
        IReadOnlyList<DocumentPersonInput>? inputs,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var links = new List<DocumentPerson>();
        if (inputs is not { Count: > 0 })
            return Result.Success<IReadOnlyList<DocumentPerson>>(links);

        var newNames = new HashSet<string>(StringComparer.Ordinal);
        int? defaultRoleId = null;

        foreach (var input in inputs)
        {
            int roleId;
            if (input.RoleId is { } requestedRoleId)
            {
                var requested = await ResolveRoleAsync(requestedRoleId, cancellationToken);
                if (requested.IsFailure)
                    return Result.Failure<IReadOnlyList<DocumentPerson>>(requested.Error);

                roleId = requested.Value;
            }
            else
            {
                if (defaultRoleId is null)
                {
                    var resolved = await ResolveDefaultRoleAsync(cancellationToken);
                    if (resolved.IsFailure)
                        return Result.Failure<IReadOnlyList<DocumentPerson>>(resolved.Error);

                    defaultRoleId = resolved.Value;
                }

                roleId = defaultRoleId.Value;
            }

            var person = await ResolvePersonAsync(input, newNames, actorUserId, utcNow, cancellationToken);
            if (person.IsFailure)
                return Result.Failure<IReadOnlyList<DocumentPerson>>(person.Error);

            var link = document.LinkPerson(person.Value, roleId, actorUserId, utcNow);
            if (link.IsFailure)
                return Result.Failure<IReadOnlyList<DocumentPerson>>(link.Error);

            links.Add(link.Value);
        }

        return Result.Success<IReadOnlyList<DocumentPerson>>(links);
    }

    /// <summary>The id of an active role, or a failure when it is unknown or deactivated.</summary>
    public async Task<Result<int>> ResolveRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        var role = await _lookupRepository.GetByIdAsync(DocumentPersonRole.LookupKey, roleId, cancellationToken);
        return role is { IsActive: true }
            ? Result.Success(role.Id)
            : Result.Failure<int>(DomainErrors.Persons.RoleNotFound);
    }

    private async Task<Result<int>> ResolveDefaultRoleAsync(CancellationToken cancellationToken)
    {
        var roles = await _lookupRepository.ListAsync(DocumentPersonRole.LookupKey, includeInactive: false, cancellationToken);
        var role = roles.FirstOrDefault(candidate => candidate.IsDefault)
            ?? roles.FirstOrDefault(candidate => candidate.Id == DocumentPersonRole.DefendantId);

        return role is null
            ? Result.Failure<int>(DomainErrors.Persons.DefaultRoleMissing)
            : Result.Success(role.Id);
    }

    private async Task<Result<Guid>> ResolvePersonAsync(
        DocumentPersonInput input,
        HashSet<string> newNames,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var hasName = !string.IsNullOrWhiteSpace(input.FullName);
        if (input.PersonId.HasValue == hasName)
            return Result.Failure<Guid>(DomainErrors.Persons.ReferenceInvalid);

        if (input.PersonId is { } personId)
        {
            var existing = await _personRepository.GetByIdAsync(personId, cancellationToken);
            return existing is null
                ? Result.Failure<Guid>(DomainErrors.Persons.NotFound)
                : Result.Success(existing.PersonId);
        }

        if (!newNames.Add(PersonNameNormalizer.Normalize(input.FullName) ?? string.Empty))
            return Result.Failure<Guid>(DomainErrors.Persons.DuplicateInDocument);

        var created = Person.Create(new PersonDetails(FullName: input.FullName), actorUserId, utcNow);
        if (created.IsFailure)
            return Result.Failure<Guid>(created.Error);

        _personRepository.Add(created.Value);
        return Result.Success(created.Value.PersonId);
    }
}
