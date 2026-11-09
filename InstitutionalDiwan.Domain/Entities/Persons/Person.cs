using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Persons;

/// <summary>
/// Someone named on documents who is not a user of the system. One row stands for one human being
/// across every document, which is what lets their documents be gathered from their name.
/// </summary>
public sealed class Person : AggregateRoot<Guid>
{
    public const int MaxFullNameLength = 300;
    public const int MaxNamePartLength = 100;
    public const int MaxDisplayNameLength = 320;
    public const int MaxNationalNumberLength = 20;
    public const int MaxCivilRegistryNumberLength = 50;
    public const int MaxPlaceLength = 200;
    public const int MaxPhoneLength = 30;
    public const int MaxAddressLength = 500;
    public const int MaxNotesLength = 2000;

    public Guid PersonId
    {
        get => Id;
        private set => Id = value;
    }

    public string? FullName { get; private set; }

    /// <summary>The name shown everywhere: <see cref="FullName"/>, else the name parts, else the national number.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary><see cref="DisplayName"/> folded by <see cref="PersonNameNormalizer"/>; the column searches run on.</summary>
    public string NormalizedName { get; private set; } = null!;

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FatherName { get; private set; }
    public string? MotherName { get; private set; }
    public string? NationalNumber { get; private set; }
    public string? CivilRegistryNumber { get; private set; }
    public string? CivilRegistryPlace { get; private set; }
    public string? BirthPlace { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    // FK → Gender.Id (InstitutionalDiwan.Domain.Entities.Persons.Gender)
    public int? GenderId { get; private set; }
    // FK → Nationality.Id (InstitutionalDiwan.Domain.Entities.Persons.Nationality)
    public int? NationalityId { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private Person()
    {
    }

    public static Result<Person> Create(PersonDetails details, Guid createdByUserId, DateTime? utcNow = null)
    {
        var person = new Person
        {
            PersonId = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            CreatedAt = utcNow ?? DateTime.UtcNow,
        };

        var applied = person.Apply(details);
        if (applied.IsFailure)
            return Result.Failure<Person>(applied.Error);

        person.RaiseDomainEvent(new PersonCreatedEvent(person.PersonId, createdByUserId));
        return Result.Success(person);
    }

    /// <summary>Replaces every recorded detail; a value left out is cleared.</summary>
    public Result UpdateDetails(PersonDetails details, Guid actorUserId, DateTime? utcNow = null)
    {
        var before = Snapshot();
        var applied = Apply(details);
        if (applied.IsFailure)
            return applied;

        UpdatedAt = utcNow ?? DateTime.UtcNow;

        var after = Snapshot();
        var changed = before.Where(pair => !Equals(pair.Value, after[pair.Key])).Select(pair => pair.Key).ToList();
        RaiseDomainEvent(new PersonDetailsUpdatedEvent(PersonId, changed, actorUserId));
        return Result.Success();
    }

    private Result Apply(PersonDetails details)
    {
        var fullName = Clean(details.FullName);
        var firstName = Clean(details.FirstName);
        var lastName = Clean(details.LastName);
        var fatherName = Clean(details.FatherName);
        var nationalNumber = Clean(details.NationalNumber);

        var nameParts = string.Join(' ', new[] { firstName, fatherName, lastName }.Where(part => part is not null));
        var displayName = fullName ?? (nameParts.Length > 0 ? nameParts : nationalNumber);
        var normalizedName = PersonNameNormalizer.Normalize(displayName);

        if (displayName is null || normalizedName is null)
            return Result.Failure(DomainErrors.Persons.Empty);

        FullName = fullName;
        DisplayName = displayName;
        NormalizedName = normalizedName;
        FirstName = firstName;
        LastName = lastName;
        FatherName = fatherName;
        MotherName = Clean(details.MotherName);
        NationalNumber = nationalNumber;
        CivilRegistryNumber = Clean(details.CivilRegistryNumber);
        CivilRegistryPlace = Clean(details.CivilRegistryPlace);
        BirthPlace = Clean(details.BirthPlace);
        BirthDate = details.BirthDate;
        GenderId = details.GenderId;
        NationalityId = details.NationalityId;
        Phone = Clean(details.Phone);
        Address = Clean(details.Address);
        Notes = Clean(details.Notes);
        return Result.Success();
    }

    // Field names only reach the audit log, so a change is traceable without copying personal data into it.
    private Dictionary<string, object?> Snapshot() => new()
    {
        [nameof(FullName)] = FullName,
        [nameof(FirstName)] = FirstName,
        [nameof(LastName)] = LastName,
        [nameof(FatherName)] = FatherName,
        [nameof(MotherName)] = MotherName,
        [nameof(NationalNumber)] = NationalNumber,
        [nameof(CivilRegistryNumber)] = CivilRegistryNumber,
        [nameof(CivilRegistryPlace)] = CivilRegistryPlace,
        [nameof(BirthPlace)] = BirthPlace,
        [nameof(BirthDate)] = BirthDate,
        [nameof(GenderId)] = GenderId,
        [nameof(NationalityId)] = NationalityId,
        [nameof(Phone)] = Phone,
        [nameof(Address)] = Address,
        [nameof(Notes)] = Notes,
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
