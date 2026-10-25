namespace InstitutionalDiwan.Domain.Entities.Audit;

/// <summary>
/// Reference table of audited action types. Replaces the former string <c>AuditLog.ActionType</c>.
/// Seeded by <c>AuditActionTypeSeeder</c>.
/// </summary>
public sealed class AuditActionType : LookupEntity
{
    public const int CreateId = 1;
    public const int UpdateId = 2;
    public const int DeleteId = 3;
    public const int ViewId = 4;
    public const int LoginId = 5;
    public const int LogoutId = 6;
    public const int ApproveId = 7;

    private AuditActionType()
    {
    }

    private AuditActionType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<AuditActionType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<AuditActionType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<AuditActionType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new AuditActionType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of audited entity types. Replaces the former string <c>AuditLog.EntityType</c>.
/// Seeded by <c>AuditEntityTypeSeeder</c>.
/// </summary>
public sealed class AuditEntityType : LookupEntity
{
    public const int DocumentId = 1;
    public const int DepartmentId = 2;
    public const int UserId = 3;
    public const int RoleId = 4;
    public const int MovementId = 5;
    public const int FileId = 6;
    public const int NumberingRegistryId = 7;
    public const int SettingId = 8;
    public const int AuditLogId = 9;
    public const int DocumentNumberId = 10;
    public const int DocumentRelationId = 11;
    public const int ExternalOrganizationId = 12;

    private AuditEntityType()
    {
    }

    private AuditEntityType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<AuditEntityType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<AuditEntityType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<AuditEntityType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new AuditEntityType(id, desc, descAr));
    }
}
