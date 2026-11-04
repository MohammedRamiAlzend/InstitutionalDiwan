using System.Globalization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Turns the registry's <c>fieldFilters=key:operator:value</c> parameters into typed filters.
/// The text is split at the first two colons only, so values such as ISO dates keep theirs.
/// </summary>
internal static class DocumentFieldFilterParser
{
    public const int MaxFilters = 5;
    private const int MaxValueLength = 500;

    public static Result<IReadOnlyList<DocumentFieldFilter>> Parse(
        IReadOnlyList<string> raw,
        IReadOnlyList<DocumentField> fields)
    {
        if (raw.Count > MaxFilters)
            return Result.Failure<IReadOnlyList<DocumentFieldFilter>>(DomainErrors.DocumentFields.TooManyFilters);

        var filters = new List<DocumentFieldFilter>(raw.Count);
        foreach (var item in raw)
        {
            var parsed = ParseOne(item, fields);
            if (parsed.IsFailure)
                return Result.Failure<IReadOnlyList<DocumentFieldFilter>>(parsed.Error);

            filters.Add(parsed.Value);
        }

        return Result.Success<IReadOnlyList<DocumentFieldFilter>>(filters);
    }

    private static Result<DocumentFieldFilter> ParseOne(string item, IReadOnlyList<DocumentField> fields)
    {
        var first = item.IndexOf(':');
        var second = first < 0 ? -1 : item.IndexOf(':', first + 1);
        if (first <= 0 || second < 0)
            return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterMalformed);

        var key = item[..first].Trim();
        var value = item[(second + 1)..].Trim();

        if (!TryParseOperator(item[(first + 1)..second].Trim(), out var op))
            return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterMalformed);

        if (value.Length == 0 || value.Length > MaxValueLength)
            return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterValueInvalid);

        var field = fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.Ordinal));
        if (field is null)
            return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterFieldNotFound);

        switch (field.DataType)
        {
            case DocumentFieldDataType.Text:
            case DocumentFieldDataType.LongText:
                if (op is not (FieldFilterOperator.Equals or FieldFilterOperator.Contains))
                    return NotSupported();
                return Result.Success(new DocumentFieldFilter(field.DocumentFieldId, field.DataType, op, Text: value));

            case DocumentFieldDataType.Number:
                if (op == FieldFilterOperator.Contains)
                    return NotSupported();
                if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out var number))
                    return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterValueInvalid);
                return Result.Success(new DocumentFieldFilter(field.DocumentFieldId, field.DataType, op, Number: number));

            case DocumentFieldDataType.Date:
                if (op == FieldFilterOperator.Contains)
                    return NotSupported();
                if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                    return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterValueInvalid);
                return Result.Success(new DocumentFieldFilter(field.DocumentFieldId, field.DataType, op, Date: date));

            case DocumentFieldDataType.Boolean:
                if (op != FieldFilterOperator.Equals)
                    return NotSupported();
                if (!bool.TryParse(value, out var boolean))
                    return Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterValueInvalid);
                return Result.Success(new DocumentFieldFilter(field.DocumentFieldId, field.DataType, op, Boolean: boolean));

            case DocumentFieldDataType.Select:
                // Option keys differ between schema versions, so an unknown key simply matches nothing.
                if (op != FieldFilterOperator.Equals)
                    return NotSupported();
                return Result.Success(new DocumentFieldFilter(field.DocumentFieldId, field.DataType, op, OptionKey: value));

            default:
                return NotSupported();
        }
    }

    private static Result<DocumentFieldFilter> NotSupported() =>
        Result.Failure<DocumentFieldFilter>(DomainErrors.DocumentFields.FilterOperatorNotSupported);

    private static bool TryParseOperator(string text, out FieldFilterOperator op)
    {
        switch (text.ToLowerInvariant())
        {
            case "eq": op = FieldFilterOperator.Equals; return true;
            case "contains": op = FieldFilterOperator.Contains; return true;
            case "gt": op = FieldFilterOperator.GreaterThan; return true;
            case "gte": op = FieldFilterOperator.GreaterThanOrEqual; return true;
            case "lt": op = FieldFilterOperator.LessThan; return true;
            case "lte": op = FieldFilterOperator.LessThanOrEqual; return true;
            default: op = default; return false;
        }
    }
}
