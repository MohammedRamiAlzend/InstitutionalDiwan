namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// The kind of value a dynamic document field holds. Stored as its integer value; the numbers must
/// never be reused or reordered once persisted.
/// </summary>
public enum DocumentFieldDataType
{
    Text = 1,
    LongText = 2,
    Number = 3,
    Date = 4,
    Boolean = 5,
    Select = 6
}
