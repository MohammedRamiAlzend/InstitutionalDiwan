namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>
/// Reference table of official-number history events (issued, cancelled, replaced, and the
/// department-register entries that carry the date written in that department's register).
/// <see cref="DocumentNumberHistory"/> stores the id.
/// </summary>
public sealed class DocumentNumberEventType : LookupEntity
{
    public const int IssuedId = 1;
    public const int CancelledId = 2;
    public const int ReplacedId = 3;
    public const int RegisteredInDepartmentId = 4;
    public const int ReRegisteredInDepartmentId = 5;

    private DocumentNumberEventType()
    {
    }

    private DocumentNumberEventType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentNumberEventType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentNumberEventType>(DomainErrors.Numbering.SequenceInvalid);

        return Result.Success(new DocumentNumberEventType(id, desc, descAr));
    }
}
