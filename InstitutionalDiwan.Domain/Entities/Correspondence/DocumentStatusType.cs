using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Reference table of document lifecycle statuses. Replaces the former string <c>StatusCode</c>;
/// documents and their status history reference it by id. The stable ids below back the status
/// transitions in <see cref="Document"/> and are seeded by <c>DocumentStatusTypeSeeder</c>.
/// </summary>
public sealed class DocumentStatusType : LookupEntity
{
    public const int DraftId = 1;
    public const int RegisteredId = 2;
    public const int InMovementId = 3;
    public const int ArchivedId = 4;
    public const int CanceledId = 5;

    /// <summary>Whether this status ends the document's lifecycle.</summary>
    public bool IsTerminal { get; private set; }

    /// <summary>Whether the status is maintained by the system rather than configured by users.</summary>
    public bool IsSystemStatus { get; private set; } = true;

    private DocumentStatusType()
    {
    }

    private DocumentStatusType(int id, string desc, string descAr, bool isTerminal, bool isSystemStatus)
        : base(id, desc, descAr)
    {
        IsTerminal = isTerminal;
        IsSystemStatus = isSystemStatus;
    }

    public static Result<DocumentStatusType> Create(
        int id,
        string? desc,
        string? descAr,
        bool isTerminal = false,
        bool isSystemStatus = true)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentStatusType>(DomainErrors.Correspondence.TypeCodeEmpty);

        return Result.Success(new DocumentStatusType(id, desc, descAr, isTerminal, isSystemStatus));
    }
}
