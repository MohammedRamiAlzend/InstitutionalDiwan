using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>Comparison applied to one dynamic field value in the document registry.</summary>
public enum FieldFilterOperator
{
    Equals = 1,
    Contains = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6
}

/// <summary>
/// One parsed registry filter on a dynamic field. Exactly the typed value matching
/// <see cref="DataType"/> is set; a document matches when it stores a value for the field that
/// satisfies the comparison.
/// </summary>
public sealed record DocumentFieldFilter(
    Guid DocumentFieldId,
    DocumentFieldDataType DataType,
    FieldFilterOperator Operator,
    string? Text = null,
    decimal? Number = null,
    DateTime? Date = null,
    bool? Boolean = null,
    string? OptionKey = null);

/// <summary>Orders the registry by one dynamic field's value; documents without a value sort together.</summary>
public sealed record DocumentFieldSort(Guid DocumentFieldId, DocumentFieldDataType DataType);
