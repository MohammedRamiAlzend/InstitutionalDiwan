using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>Reference table of numbering sequence reset policies. Registries store the id.</summary>
public sealed class NumberingResetPolicy : LookupEntity
{
    public const int YearlyId = 1;
    public const int MonthlyId = 2;
    public const int NeverId = 3;

    private NumberingResetPolicy()
    {
    }

    private NumberingResetPolicy(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<NumberingResetPolicy> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NumberingResetPolicy>(DomainErrors.Numbering.RegistryCodeEmpty);

        return Result.Success(new NumberingResetPolicy(id, desc, descAr));
    }
}
