using InstitutionalDiwan.Domain.Entities.Audit;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the audit action type lookup (Order 61, every environment).</summary>
public sealed class AuditActionTypeSeeder : LookupSeederBase<AuditActionType>
{
    public override int Order => 61;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (AuditActionType.CreateId, "Create", "إنشاء"),
        (AuditActionType.UpdateId, "Update", "تعديل"),
        (AuditActionType.DeleteId, "Delete", "حذف"),
        (AuditActionType.ViewId, "View", "عرض"),
        (AuditActionType.LoginId, "Login", "تسجيل دخول"),
        (AuditActionType.LogoutId, "Logout", "تسجيل خروج"),
        (AuditActionType.ApproveId, "Approve", "اعتماد"),
    };

    protected override AuditActionType BuildItem(int id, string desc, string descAr) =>
        AuditActionType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the audit entity type lookup (Order 62, every environment).</summary>
public sealed class AuditEntityTypeSeeder : LookupSeederBase<AuditEntityType>
{
    public override int Order => 62;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (AuditEntityType.DocumentId, "Document", "وثيقة"),
        (AuditEntityType.DepartmentId, "Department", "دائرة"),
        (AuditEntityType.UserId, "User", "مستخدم"),
        (AuditEntityType.RoleId, "Role", "دور"),
        (AuditEntityType.MovementId, "Movement", "إحالة"),
        (AuditEntityType.FileId, "File", "ملف"),
        (AuditEntityType.NumberingRegistryId, "Numbering registry", "سجل ترقيم"),
        (AuditEntityType.SettingId, "Setting", "إعداد"),
    };

    protected override AuditEntityType BuildItem(int id, string desc, string descAr) =>
        AuditEntityType.Create(id, desc, descAr).Value;
}
