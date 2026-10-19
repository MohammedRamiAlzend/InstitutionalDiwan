namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Reference table of the role a participant plays on a document. Replaces the former string
/// <c>DocumentParticipant.RoleCode</c>. Seeded by <c>ParticipantRoleSeeder</c>.
/// </summary>
public sealed class ParticipantRole : LookupEntity
{
    public const int SenderId = 1;
    public const int ReceiverId = 2;
    public const int CcId = 3;
    public const int SignatoryId = 4;

    private ParticipantRole()
    {
    }

    private ParticipantRole(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<ParticipantRole> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<ParticipantRole>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<ParticipantRole>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new ParticipantRole(id, desc, descAr));
    }
}

/// <summary>
/// Reference table describing what a participant points at (an internal department, an external
/// organization, or a named person). Replaces the former string <c>DocumentParticipant.TargetType</c>.
/// Seeded by <c>ParticipantTargetTypeSeeder</c>.
/// </summary>
public sealed class ParticipantTargetType : LookupEntity
{
    public const int ExternalOrganizationId = 1;
    public const int InternalDepartmentId = 2;
    public const int PersonId = 3;

    private ParticipantTargetType()
    {
    }

    private ParticipantTargetType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<ParticipantTargetType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<ParticipantTargetType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<ParticipantTargetType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new ParticipantTargetType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table classifying an external organization. Replaces the former string
/// <c>ExternalOrganization.OrganizationType</c>. Seeded by <c>OrganizationTypeSeeder</c>.
/// </summary>
public sealed class OrganizationType : LookupEntity
{
    public const int GovernmentId = 1;
    public const int PrivateId = 2;
    public const int InternationalId = 3;
    public const int IndividualId = 4;

    private OrganizationType()
    {
    }

    private OrganizationType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<OrganizationType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<OrganizationType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<OrganizationType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new OrganizationType(id, desc, descAr));
    }
}
