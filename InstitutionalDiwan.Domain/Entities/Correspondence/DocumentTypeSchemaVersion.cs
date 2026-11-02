using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// One immutable-once-published version of the dynamic field schema of a document type. Administrators
/// edit a single <c>Draft</c> version and publish it; a published version is frozen. A document is
/// pinned to the published version that was current when it was created and never moves, so later
/// schema changes only affect documents created afterwards.
/// </summary>
public sealed class DocumentTypeSchemaVersion : AggregateRoot<Guid>
{
    public const int MaxFields = 50;

    private readonly List<DocumentTypeSchemaVersionField> _fields = new();

    public Guid SchemaVersionId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentType)
    public int DocumentTypeId { get; private set; }
    public int VersionNumber { get; private set; }
    public DocumentTypeSchemaVersionStatus Status { get; private set; } = DocumentTypeSchemaVersionStatus.Draft;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public IReadOnlyCollection<DocumentTypeSchemaVersionField> Fields => _fields.AsReadOnly();

    private DocumentTypeSchemaVersion()
    {
    }

    /// <summary>Starts an empty draft version.</summary>
    public static Result<DocumentTypeSchemaVersion> CreateDraft(
        int documentTypeId,
        int versionNumber,
        Guid createdByUserId,
        DateTime? utcNow = null)
    {
        if (documentTypeId <= 0)
            return Result.Failure<DocumentTypeSchemaVersion>(DomainErrors.DocumentFields.DocumentTypeInvalid);

        return Result.Success(new DocumentTypeSchemaVersion
        {
            SchemaVersionId = Guid.NewGuid(),
            DocumentTypeId = documentTypeId,
            VersionNumber = Math.Max(versionNumber, 1),
            Status = DocumentTypeSchemaVersionStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = utcNow ?? DateTime.UtcNow
        });
    }

    /// <summary>Starts a draft that begins as a copy of <paramref name="basis"/>'s fields, so a change is an edit, not a rewrite.</summary>
    public static Result<DocumentTypeSchemaVersion> CreateDraftFrom(
        DocumentTypeSchemaVersion basis,
        int versionNumber,
        Guid createdByUserId,
        DateTime? utcNow = null)
    {
        var draftResult = CreateDraft(basis.DocumentTypeId, versionNumber, createdByUserId, utcNow);
        if (draftResult.IsFailure)
            return draftResult;

        var draft = draftResult.Value;
        // Copies keep each field's identity (DocumentFieldId) so stored values stay linked across versions.
        foreach (var field in basis._fields)
            draft._fields.Add(field.CloneInto(draft.SchemaVersionId));

        return Result.Success(draft);
    }

    public Result AddField(DocumentField field, DocumentFieldSettings settings, DateTime? utcNow = null)
    {
        var editable = EnsureDraft();
        if (editable.IsFailure)
            return editable;

        if (field.DocumentTypeId != DocumentTypeId)
            return Result.Failure(DomainErrors.DocumentFields.FieldDocumentTypeMismatch);

        if (settings.DataType != field.DataType)
            return Result.Failure(DomainErrors.DocumentFields.FieldTypeMismatch);

        if (_fields.Any(f => f.DocumentFieldId == field.DocumentFieldId))
            return Result.Failure(DomainErrors.DocumentFields.FieldAlreadyInVersion);

        if (_fields.Count >= MaxFields)
            return Result.Failure(DomainErrors.DocumentFields.TooManyFields);

        _fields.Add(DocumentTypeSchemaVersionField.Create(SchemaVersionId, field, settings));
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateField(Guid documentFieldId, DocumentFieldSettings settings, DateTime? utcNow = null)
    {
        var editable = EnsureDraft();
        if (editable.IsFailure)
            return editable;

        var field = _fields.FirstOrDefault(f => f.DocumentFieldId == documentFieldId);
        if (field is null)
            return Result.Failure(DomainErrors.DocumentFields.FieldNotInVersion);

        if (settings.DataType != field.DataType)
            return Result.Failure(DomainErrors.DocumentFields.FieldTypeMismatch);

        field.Apply(settings);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Drops a field from this draft; the field's stored values and identity are untouched.</summary>
    public Result RemoveField(Guid documentFieldId, DateTime? utcNow = null)
    {
        var editable = EnsureDraft();
        if (editable.IsFailure)
            return editable;

        var field = _fields.FirstOrDefault(f => f.DocumentFieldId == documentFieldId);
        if (field is null)
            return Result.Failure(DomainErrors.DocumentFields.FieldNotInVersion);

        _fields.Remove(field);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Freezes the draft. The caller retires the previously published version of the type.</summary>
    public Result Publish(Guid actorUserId, DateTime? utcNow = null)
    {
        var editable = EnsureDraft();
        if (editable.IsFailure)
            return editable;

        var now = utcNow ?? DateTime.UtcNow;
        Status = DocumentTypeSchemaVersionStatus.Published;
        PublishedAt = now;
        PublishedByUserId = actorUserId;
        UpdatedAt = now;

        RaiseDomainEvent(new DocumentSchemaVersionPublishedEvent(SchemaVersionId, DocumentTypeId, VersionNumber, actorUserId));
        return Result.Success();
    }

    /// <summary>Marks a superseded published version; documents already pinned to it keep using it.</summary>
    public Result Retire(DateTime? utcNow = null)
    {
        if (Status != DocumentTypeSchemaVersionStatus.Published)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionNotPublished);

        Status = DocumentTypeSchemaVersionStatus.Retired;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Validates raw submitted values against this version and returns the typed rows to store.
    /// Blank values are skipped (a draft may be incomplete); an unknown or repeated key is rejected.
    /// Required-field completeness is a separate gate, <see cref="CheckRequiredFields"/>.
    /// </summary>
    public Result<IReadOnlyList<DocumentFieldValue>> BuildValues(
        Guid documentId,
        IReadOnlyCollection<DocumentFieldValueInput> inputs)
    {
        var values = new List<DocumentFieldValue>(inputs.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in inputs)
        {
            var key = input.FieldKey?.Trim() ?? string.Empty;
            var field = _fields.FirstOrDefault(f => f.FieldKey == key);
            if (field is null)
                return Result.Failure<IReadOnlyList<DocumentFieldValue>>(DomainErrors.DocumentFields.UnknownField(key));

            if (!seen.Add(key))
                return Result.Failure<IReadOnlyList<DocumentFieldValue>>(DomainErrors.DocumentFields.DuplicateValue);

            if (string.IsNullOrWhiteSpace(input.Value))
                continue;

            var valueResult = field.BuildValue(documentId, input.Value);
            if (valueResult.IsFailure)
                return Result.Failure<IReadOnlyList<DocumentFieldValue>>(valueResult.Error);

            values.Add(valueResult.Value);
        }

        return Result.Success<IReadOnlyList<DocumentFieldValue>>(values);
    }

    /// <summary>Fails with the first required field that has no stored value; run before registering a document.</summary>
    public Result CheckRequiredFields(IEnumerable<DocumentFieldValue> values)
    {
        var present = values.Select(v => v.DocumentFieldId).ToHashSet();
        var missing = _fields
            .Where(f => f.IsRequired && !present.Contains(f.DocumentFieldId))
            .OrderBy(f => f.DisplayOrder)
            .FirstOrDefault();

        return missing is null
            ? Result.Success()
            : Result.Failure(DomainErrors.DocumentFields.ValueRequired(missing.FieldKey));
    }

    private Result EnsureDraft() =>
        Status == DocumentTypeSchemaVersionStatus.Draft
            ? Result.Success()
            : Result.Failure(DomainErrors.DocumentFields.SchemaVersionNotDraft);
}
