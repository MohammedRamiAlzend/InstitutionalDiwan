using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the user scope type lookup (Order 41, every environment).</summary>
public sealed class UserScopeTypeSeeder : LookupSeederBase<UserScopeType>
{
    public override int Order => 41;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (UserScopeType.DepartmentId, "Own department", "الدائرة"),
        (UserScopeType.DepartmentTreeId, "Department and sub-departments", "الدائرة والفروع"),
        (UserScopeType.InstitutionId, "Whole institution", "المؤسسة"),
        (UserScopeType.SystemId, "System", "النظام"),
    };

    protected override UserScopeType BuildItem(int id, string desc, string descAr) =>
        UserScopeType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the department assignment type lookup (Order 42, every environment).</summary>
public sealed class AssignmentTypeSeeder : LookupSeederBase<AssignmentType>
{
    public override int Order => 42;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (AssignmentType.PrimaryId, "Primary", "أساسي"),
        (AssignmentType.AdditionalId, "Additional", "إضافي"),
    };

    protected override AssignmentType BuildItem(int id, string desc, string descAr) =>
        AssignmentType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the document access level lookup (Order 43, every environment).</summary>
public sealed class AccessLevelSeeder : LookupSeederBase<AccessLevel>
{
    public override int Order => 43;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (AccessLevel.ViewId, "View", "عرض"),
        (AccessLevel.DownloadId, "Download", "تنزيل"),
        (AccessLevel.ForwardId, "Forward", "إحالة"),
        (AccessLevel.ManageId, "Manage", "إدارة"),
    };

    protected override AccessLevel BuildItem(int id, string desc, string descAr) =>
        AccessLevel.Create(id, desc, descAr).Value;
}
