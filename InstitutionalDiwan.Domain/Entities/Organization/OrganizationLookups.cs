namespace InstitutionalDiwan.Domain.Entities.Organization;

/// <summary>
/// Reference table describing how official numbers are allocated. Replaces the former string
/// <c>InstitutionSetting.NumberingMode</c>. Seeded by <c>NumberingModeSeeder</c>.
/// </summary>
public sealed class NumberingMode : LookupEntity
{
    public const int AutomaticId = 1;
    public const int ManualId = 2;

    private NumberingMode()
    {
    }

    private NumberingMode(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<NumberingMode> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<NumberingMode>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NumberingMode>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new NumberingMode(id, desc, descAr));
    }
}
