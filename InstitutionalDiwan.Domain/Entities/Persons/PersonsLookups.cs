namespace InstitutionalDiwan.Domain.Entities.Persons;

/// <summary>
/// Reference table of the capacity a person holds on a document (defendant, plaintiff, ...). Seeded
/// by <c>DocumentPersonRoleSeeder</c>; further entries are added from the lookups screen.
/// </summary>
public sealed class DocumentPersonRole : LookupEntity
{
    /// <summary>Route segment of this catalog under <c>/api/lookups</c>.</summary>
    public const string LookupKey = "document-person-roles";

    public const int DefendantId = 1;
    public const int PlaintiffId = 2;

    private DocumentPersonRole()
    {
    }

    private DocumentPersonRole(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentPersonRole> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<DocumentPersonRole>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentPersonRole>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new DocumentPersonRole(id, desc, descAr));
    }
}

/// <summary>Reference table of a person's gender. Seeded by <c>GenderSeeder</c>.</summary>
public sealed class Gender : LookupEntity
{
    /// <summary>Route segment of this catalog under <c>/api/lookups</c>.</summary>
    public const string LookupKey = "genders";

    public const int MaleId = 1;
    public const int FemaleId = 2;

    private Gender()
    {
    }

    private Gender(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<Gender> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<Gender>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<Gender>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new Gender(id, desc, descAr));
    }
}

/// <summary>Reference table of nationalities. Starts empty and is filled from the lookups screen.</summary>
public sealed class Nationality : LookupEntity
{
    /// <summary>Route segment of this catalog under <c>/api/lookups</c>.</summary>
    public const string LookupKey = "nationalities";

    private Nationality()
    {
    }

    private Nationality(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<Nationality> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<Nationality>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<Nationality>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new Nationality(id, desc, descAr));
    }
}
