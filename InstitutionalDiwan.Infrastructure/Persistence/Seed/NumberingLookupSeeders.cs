using InstitutionalDiwan.Domain.Entities.Numbering;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the official-number status lookup (Order 80, every environment).</summary>
public sealed class DocumentNumberStatusSeeder : LookupSeederBase<DocumentNumberStatus>
{
    public override int Order => 80;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentNumberStatus.ActiveId, "Active", "فعّال"),
        (DocumentNumberStatus.CancelledId, "Cancelled", "ملغي"),
        (DocumentNumberStatus.ReplacedId, "Replaced", "مستبدل"),
    };

    protected override DocumentNumberStatus BuildItem(int id, string desc, string descAr) =>
        DocumentNumberStatus.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the official-number history event lookup (Order 85, every environment).</summary>
public sealed class DocumentNumberEventTypeSeeder : LookupSeederBase<DocumentNumberEventType>
{
    public override int Order => 85;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentNumberEventType.IssuedId, "Issued", "إصدار"),
        (DocumentNumberEventType.CancelledId, "Cancelled", "إلغاء"),
        (DocumentNumberEventType.ReplacedId, "Replaced", "استبدال"),
    };

    protected override DocumentNumberEventType BuildItem(int id, string desc, string descAr) =>
        DocumentNumberEventType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the numbering reset policy lookup (Order 90, every environment).</summary>
public sealed class NumberingResetPolicySeeder : LookupSeederBase<NumberingResetPolicy>
{
    public override int Order => 90;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NumberingResetPolicy.YearlyId, "Yearly", "سنوي"),
        (NumberingResetPolicy.MonthlyId, "Monthly", "شهري"),
        (NumberingResetPolicy.NeverId, "Never", "لا يتصفّر"),
    };

    protected override NumberingResetPolicy BuildItem(int id, string desc, string descAr) =>
        NumberingResetPolicy.Create(id, desc, descAr).Value;
}
