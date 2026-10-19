namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>Reference table of document priority levels. Documents store the integer id.</summary>
public sealed class DocumentPriorityLevel : LookupEntity
{
    public const int NormalId = 1;
    public const int UrgentId = 2;
    public const int ImmediateId = 3;

    private DocumentPriorityLevel()
    {
    }

    private DocumentPriorityLevel(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentPriorityLevel> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentPriorityLevel>(DomainErrors.Correspondence.TitleEmpty);

        return Result.Success(new DocumentPriorityLevel(id, desc, descAr));
    }
}
