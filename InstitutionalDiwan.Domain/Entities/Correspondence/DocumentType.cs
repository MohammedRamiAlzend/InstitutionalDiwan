namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Reference table of document types (incoming letter, internal memo, ...). Replaces the former string
/// <c>DocumentTypeCode</c>; documents and numbering registries reference it by id. Default rows are
/// seeded by <c>DocumentTypeSeeder</c>.
/// </summary>
public sealed class DocumentType : LookupEntity
{
    public const int IncomingLetterId = 1;
    public const int OutgoingLetterId = 2;
    public const int InternalMemoId = 3;
    public const int CircularId = 4;
    public const int DecisionId = 5;

    /// <summary>FK → DocumentDirection.Id; the only direction this type may be used with (or ALL).</summary>
    public int AllowedDirectionId { get; private set; } = DocumentDirection.AllId;

    /// <summary>Whether documents of this type must be issued an official number.</summary>
    public bool RequiresOfficialNumber { get; private set; } = true;

    private DocumentType()
    {
    }

    private DocumentType(
        int id,
        string desc,
        string descAr,
        int allowedDirectionId,
        bool requiresOfficialNumber) : base(id, desc, descAr)
    {
        AllowedDirectionId = allowedDirectionId;
        RequiresOfficialNumber = requiresOfficialNumber;
    }

    public static Result<DocumentType> Create(
        int id,
        string? desc,
        string? descAr,
        int allowedDirectionId = DocumentDirection.AllId,
        bool requiresOfficialNumber = true)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentType>(DomainErrors.Correspondence.TypeCodeEmpty);

        return Result.Success(new DocumentType(id, desc, descAr, allowedDirectionId, requiresOfficialNumber));
    }
}
