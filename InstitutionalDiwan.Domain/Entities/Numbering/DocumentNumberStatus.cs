using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>Reference table of official-number statuses. <see cref="DocumentNumber"/> stores the id.</summary>
public sealed class DocumentNumberStatus : LookupEntity
{
    public const int ActiveId = 1;
    public const int CancelledId = 2;
    public const int ReplacedId = 3;

    private DocumentNumberStatus()
    {
    }

    private DocumentNumberStatus(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<DocumentNumberStatus> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentNumberStatus>(DomainErrors.Numbering.SequenceInvalid);

        return Result.Success(new DocumentNumberStatus(id, desc, descAr));
    }
}
