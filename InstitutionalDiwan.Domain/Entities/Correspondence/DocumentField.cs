namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// The stable identity of one dynamic field of a document type. The <see cref="Key"/> and
/// <see cref="DataType"/> never change after creation, so stored values stay meaningful across every
/// schema version; label, requiredness and constraints live in the per-version settings instead.
/// </summary>
public sealed class DocumentField : Entity<Guid>
{
    public const int MaxKeyLength = 50;

    public Guid DocumentFieldId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentType)
    public int DocumentTypeId { get; private set; }
    public string Key { get; private set; } = null!;
    public DocumentFieldDataType DataType { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentField()
    {
    }

    public static Result<DocumentField> Create(
        int documentTypeId,
        string? key,
        DocumentFieldDataType dataType,
        Guid createdByUserId,
        DateTime? utcNow = null)
    {
        if (documentTypeId <= 0)
            return Result.Failure<DocumentField>(DomainErrors.DocumentFields.DocumentTypeInvalid);

        var normalizedKey = key?.Trim();
        if (!IsValidKey(normalizedKey))
            return Result.Failure<DocumentField>(DomainErrors.DocumentFields.KeyInvalid);

        if (!Enum.IsDefined(dataType))
            return Result.Failure<DocumentField>(DomainErrors.DocumentFields.DataTypeInvalid);

        return Result.Success(new DocumentField
        {
            DocumentFieldId = Guid.NewGuid(),
            DocumentTypeId = documentTypeId,
            Key = normalizedKey!,
            DataType = dataType,
            CreatedByUserId = createdByUserId,
            CreatedAt = utcNow ?? DateTime.UtcNow
        });
    }

    /// <summary>A key (field or option) starts with a lowercase letter and holds only <c>a-z 0-9 _</c>, up to 50 characters.</summary>
    public static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > MaxKeyLength || key[0] is < 'a' or > 'z')
            return false;

        foreach (var c in key)
        {
            if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '_'))
                return false;
        }

        return true;
    }
}
