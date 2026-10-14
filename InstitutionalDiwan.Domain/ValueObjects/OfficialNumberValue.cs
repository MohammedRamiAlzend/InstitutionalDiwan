using System.Collections.Generic;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.ValueObjects;

public sealed class OfficialNumberValue : ValueObject
{
    public string Value { get; }

    private OfficialNumberValue(string value)
    {
        Value = value;
    }

    public static Result<OfficialNumberValue> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<OfficialNumberValue>(DomainErrors.Numbering.OfficialNumberEmpty);

        var normalized = value.Trim();
        if (normalized.Length > 100)
            return Result.Failure<OfficialNumberValue>(DomainErrors.Numbering.OfficialNumberEmpty);

        return Result.Success(new OfficialNumberValue(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
