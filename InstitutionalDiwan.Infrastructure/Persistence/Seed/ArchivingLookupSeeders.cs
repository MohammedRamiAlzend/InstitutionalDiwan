using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the archive event type lookup (Order 58, every environment).</summary>
public sealed class ArchiveEventTypeSeeder : LookupSeederBase<ArchiveEventType>
{
    public override int Order => 58;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (ArchiveEventType.ArchivedId, "Archived", "أرشفة"),
        (ArchiveEventType.UnarchivedId, "Unarchived", "إلغاء الأرشفة"),
        (ArchiveEventType.RetentionAppliedId, "Retention applied", "تطبيق مدة الحفظ"),
    };

    protected override ArchiveEventType BuildItem(int id, string desc, string descAr) =>
        ArchiveEventType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the archive location lookup (Order 59, every environment).</summary>
public sealed class ArchiveLocationSeeder : LookupSeederBase<ArchiveLocation>
{
    public override int Order => 59;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (ArchiveLocation.CentralRegistryId, "Central registry", "الديوان المركزي"),
        (ArchiveLocation.PhysicalArchiveId, "Physical archive", "الأرشيف الورقي"),
        (ArchiveLocation.ElectronicArchiveId, "Electronic archive", "الأرشيف الإلكتروني"),
    };

    protected override ArchiveLocation BuildItem(int id, string desc, string descAr) =>
        ArchiveLocation.Create(id, desc, descAr).Value;
}
