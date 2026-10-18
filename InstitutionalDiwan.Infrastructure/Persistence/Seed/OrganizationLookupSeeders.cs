using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the institution numbering mode lookup (Order 68, every environment).</summary>
public sealed class NumberingModeSeeder : LookupSeederBase<NumberingMode>
{
    public override int Order => 68;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NumberingMode.AutomaticId, "Automatic", "تلقائي"),
        (NumberingMode.ManualId, "Manual", "يدوي"),
    };

    protected override NumberingMode BuildItem(int id, string desc, string descAr) =>
        NumberingMode.Create(id, desc, descAr).Value;
}
