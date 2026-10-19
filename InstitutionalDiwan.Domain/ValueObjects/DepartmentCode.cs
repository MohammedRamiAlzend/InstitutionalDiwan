namespace InstitutionalDiwan.Domain.ValueObjects;

public sealed class DepartmentCode : ValueObject
{
    public string Value { get; }

    private DepartmentCode(string value)
    {
        Value = value;
    }

    public static Result<DepartmentCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<DepartmentCode>(DomainErrors.Department.CodeEmpty);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            return Result.Failure<DepartmentCode>(DomainErrors.Department.CodeTooLong);

        return Result.Success(new DepartmentCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
