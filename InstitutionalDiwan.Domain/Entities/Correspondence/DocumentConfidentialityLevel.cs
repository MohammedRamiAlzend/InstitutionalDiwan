namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>Reference table of document confidentiality levels. Documents store the integer id.</summary>
public sealed class DocumentConfidentialityLevel : LookupEntity
{
    public const int NormalId = 1;
    public const int ConfidentialId = 2;
    public const int SecretId = 3;
    public const int TopSecretId = 4;

    private DocumentConfidentialityLevel()
    {
    }

    private DocumentConfidentialityLevel(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentConfidentialityLevel> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentConfidentialityLevel>(DomainErrors.Correspondence.TitleEmpty);

        return Result.Success(new DocumentConfidentialityLevel(id, desc, descAr));
    }
}
