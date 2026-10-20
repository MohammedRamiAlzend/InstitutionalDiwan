namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>
/// Reference table of register scopes. A <see cref="DocumentNumber"/> is either issued by the
/// central Diwan register or written into a department's own register when the document reaches
/// that department.
/// </summary>
public sealed class DocumentNumberScope : LookupEntity
{
    public const int CentralId = 1;
    public const int DepartmentId = 2;

    private DocumentNumberScope()
    {
    }

    private DocumentNumberScope(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentNumberScope> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentNumberScope>(DomainErrors.Numbering.RegistryCodeEmpty);

        return Result.Success(new DocumentNumberScope(id, desc, descAr));
    }
}
