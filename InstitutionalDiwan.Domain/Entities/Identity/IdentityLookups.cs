namespace InstitutionalDiwan.Domain.Entities.Identity;

/// <summary>
/// Reference table describing how far a user's data visibility extends. Replaces the former string
/// <c>User.ScopeType</c>. Seeded by <c>UserScopeTypeSeeder</c>.
/// </summary>
public sealed class UserScopeType : LookupEntity
{
    public const int DepartmentId = 1;
    public const int DepartmentTreeId = 2;
    public const int InstitutionId = 3;
    public const int SystemId = 4;

    private UserScopeType()
    {
    }

    private UserScopeType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<UserScopeType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<UserScopeType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<UserScopeType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new UserScopeType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table classifying a user-to-department assignment. Replaces the former string
/// <c>UserDepartmentAssignment.AssignmentType</c>. Seeded by <c>AssignmentTypeSeeder</c>.
/// </summary>
public sealed class AssignmentType : LookupEntity
{
    public const int PrimaryId = 1;
    public const int AdditionalId = 2;

    private AssignmentType()
    {
    }

    private AssignmentType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<AssignmentType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<AssignmentType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<AssignmentType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new AssignmentType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of access levels that can be granted over a document. Replaces the former string
/// <c>DocumentAccessGrant.AccessLevel</c>. Seeded by <c>AccessLevelSeeder</c>.
/// </summary>
public sealed class AccessLevel : LookupEntity
{
    public const int ViewId = 1;
    public const int DownloadId = 2;
    public const int ForwardId = 3;
    public const int ManageId = 4;

    private AccessLevel()
    {
    }

    private AccessLevel(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<AccessLevel> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<AccessLevel>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<AccessLevel>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new AccessLevel(id, desc, descAr));
    }
}
