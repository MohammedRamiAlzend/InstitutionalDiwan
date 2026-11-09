using InstitutionalDiwan.Domain.Entities.Persons;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the capacities a person can hold on a document (Order 91, every environment).</summary>
public sealed class DocumentPersonRoleSeeder : LookupSeederBase<DocumentPersonRole>
{
    public override int Order => 91;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentPersonRole.DefendantId, "Defendant", "مدعى عليه"),
        (DocumentPersonRole.PlaintiffId, "Plaintiff", "مدعٍ"),
    };

    protected override DocumentPersonRole BuildItem(int id, string desc, string descAr)
    {
        var role = DocumentPersonRole.Create(id, desc, descAr).Value;
        if (id == DocumentPersonRole.DefendantId)
            role.MarkDefault();

        return role;
    }
}

/// <summary>Seeds the gender lookup (Order 92, every environment).</summary>
public sealed class GenderSeeder : LookupSeederBase<Gender>
{
    public override int Order => 92;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (Gender.MaleId, "Male", "ذكر"),
        (Gender.FemaleId, "Female", "أنثى"),
    };

    protected override Gender BuildItem(int id, string desc, string descAr) =>
        Gender.Create(id, desc, descAr).Value;
}
