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
    /// <summary>
    /// FK → Department.DepartmentId. Null for the creation form of the type; otherwise the record form
    /// the department fills on its visits of documents of this type. Versions are numbered, drafted and
    /// published per (type, department).
    /// </summary>
    public Guid? DepartmentId { get; private set; }
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
        DateTime? utcNow = null,
        Guid? departmentId = null)
    {
        if (documentTypeId <= 0)
            return Result.Failure<DocumentTypeSchemaVersion>(DomainErrors.DocumentFields.DocumentTypeInvalid);

        if (departmentId == Guid.Empty)
            return Result.Failure<DocumentTypeSchemaVersion>(DomainErrors.Department.NotFound);

        return Result.Success(new DocumentTypeSchemaVersion
        {
            SchemaVersionId = Guid.NewGuid(),
            DocumentTypeId = documentTypeId,
            DepartmentId = departmentId,
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
        var draftResult = CreateDraft(basis.DocumentTypeId, versionNumber, createdByUserId, utcNow, basis.DepartmentId);
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

        // A field belongs to one form: the type's creation form (no department) or one department's record form.
        if (field.DocumentTypeId != DocumentTypeId || field.DepartmentId != DepartmentId)
            return Result.Failure(DomainErrors.DocumentFields.FieldDocumentTypeMismatch);

        if (settings.DataType != field.DataType)
            return Result.Failure(DomainErrors.DocumentFields.FieldTypeMismatch);

        if (_fields.Any(f => f.DocumentFieldId == field.DocumentFieldId))
            return Result.Failure(DomainErrors.DocumentFields.FieldAlreadyInVersion);

        if (_fields.Count >= MaxFields)
            return Result.Failure(DomainErrors.DocumentFields.TooManyFields);

        var conditions = CheckConditions(field.DocumentFieldId, field.Key, field.DataType, settings);
        if (conditions.IsFailure)
            return conditions;

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

        var conditions = CheckConditions(field.DocumentFieldId, field.FieldKey, field.DataType, settings);
        if (conditions.IsFailure)
            return conditions;

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

        if (_fields.Any(other => other.Behavior.VisibleWhenFieldKey == field.FieldKey))
            return Result.Failure(DomainErrors.DocumentFields.FieldHasDependents);

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
    /// Read-only fields always get their fixed value and fields hidden by a condition are not kept.
    /// Required-field completeness is a separate gate, <see cref="CheckRequiredFields"/>.
    /// </summary>
    /// <param name="existing">The document's stored values, so a read-only "today" keeps its original date.</param>
    /// <param name="sourcedOptions">Current choices of the fields fed by a source, by field id.</param>
    public Result<IReadOnlyList<DocumentFieldValue>> BuildValues(
        Guid documentId,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        IReadOnlyCollection<DocumentFieldValue>? existing = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var built = new Dictionary<Guid, DocumentFieldValue>(inputs.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var fixedValues = BuildFixedValues(documentId, existing, now);
        if (fixedValues.IsFailure)
            return Result.Failure<IReadOnlyList<DocumentFieldValue>>(fixedValues.Error);

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

            var valueResult = field.BuildValue(documentId, input.Value, OptionsOf(field, sourcedOptions), now);
            if (valueResult.IsFailure)
                return Result.Failure<IReadOnlyList<DocumentFieldValue>>(valueResult.Error);

            if (field.Behavior.IsReadOnly)
            {
                if (!valueResult.Value.HasSameContent(fixedValues.Value[field.DocumentFieldId]))
                    return Result.Failure<IReadOnlyList<DocumentFieldValue>>(DomainErrors.DocumentFields.FieldReadOnly(key));
                continue;
            }

            built[field.DocumentFieldId] = valueResult.Value;
        }

        foreach (var (fieldId, value) in fixedValues.Value)
            built[fieldId] = value;

        return Result.Success<IReadOnlyList<DocumentFieldValue>>(
            _fields.Where(field => built.ContainsKey(field.DocumentFieldId) && IsVisible(field, built))
                .Select(field => built[field.DocumentFieldId])
                .ToList());
    }

    /// <summary>The read-only values a document lacks; a document created without inputs still needs them.</summary>
    public Result<IReadOnlyList<DocumentFieldValue>> BuildMissingFixedValues(
        Guid documentId,
        IReadOnlyCollection<DocumentFieldValue> existing,
        DateTime utcNow)
    {
        var fixedValues = BuildFixedValues(documentId, existing, utcNow);
        if (fixedValues.IsFailure)
            return Result.Failure<IReadOnlyList<DocumentFieldValue>>(fixedValues.Error);

        var known = existing.ToDictionary(value => value.DocumentFieldId);
        foreach (var (fieldId, value) in fixedValues.Value)
            known[fieldId] = value;

        return Result.Success<IReadOnlyList<DocumentFieldValue>>(
            _fields.Where(field => field.Behavior.IsReadOnly && IsVisible(field, known))
                .Select(field => known[field.DocumentFieldId])
                .Where(value => existing.All(stored => stored.DocumentFieldId != value.DocumentFieldId))
                .ToList());
    }

    /// <summary>Fails with the first required visible field that has no stored value; run before registering a document.</summary>
    public Result CheckRequiredFields(IEnumerable<DocumentFieldValue> values)
    {
        var present = values.ToDictionary(value => value.DocumentFieldId);
        var missing = _fields
            .Where(f => f.IsRequired && !present.ContainsKey(f.DocumentFieldId) && IsVisible(f, present))
            .OrderBy(f => f.DisplayOrder)
            .FirstOrDefault();

        return missing is null
            ? Result.Success()
            : Result.Failure(DomainErrors.DocumentFields.ValueRequired(missing.FieldKey));
    }

    private Result<Dictionary<Guid, DocumentFieldValue>> BuildFixedValues(
        Guid documentId,
        IReadOnlyCollection<DocumentFieldValue>? existing,
        DateTime utcNow)
    {
        var values = new Dictionary<Guid, DocumentFieldValue>();
        foreach (var field in _fields.Where(f => f.Behavior.IsReadOnly))
        {
            var stored = existing?.FirstOrDefault(value => value.DocumentFieldId == field.DocumentFieldId);
            if (stored is not null)
            {
                values[field.DocumentFieldId] = stored;
                continue;
            }

            var built = field.BuildFixedValue(documentId, utcNow);
            if (built.IsFailure)
                return Result.Failure<Dictionary<Guid, DocumentFieldValue>>(built.Error);

            values[field.DocumentFieldId] = built.Value;
        }

        return Result.Success(values);
    }

    private static IReadOnlyList<DocumentFieldOption>? OptionsOf(
        DocumentTypeSchemaVersionField field,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions) =>
        sourcedOptions is not null && sourcedOptions.TryGetValue(field.DocumentFieldId, out var options)
            ? options
            : null;

    /// <summary>A field shows when its condition holds and the field it depends on shows too.</summary>
    private bool IsVisible(
        DocumentTypeSchemaVersionField field,
        IReadOnlyDictionary<Guid, DocumentFieldValue> values)
    {
        // Every link was checked to be acyclic when configured, so this walk ends within the field count.
        var current = field;
        for (var hop = 0; hop <= _fields.Count; hop++)
        {
            if (current.Behavior.VisibleWhenFieldKey is not { } controllerKey)
                return true;

            var controller = _fields.FirstOrDefault(f => f.FieldKey == controllerKey);
            if (controller is null)
                return true;

            if (!values.TryGetValue(controller.DocumentFieldId, out var value)
                || !value.MatchesChoice(current.Behavior.VisibleWhenValue!))
            {
                return false;
            }

            current = controller;
        }

        return false;
    }

    private sealed record ConditionNode(
        string Key,
        DocumentFieldDataType DataType,
        DocumentFieldBehavior Behavior,
        IReadOnlyList<DocumentFieldOption> Options);

    /// <summary>Checks every visibility condition as it would stand once the changed field is applied.</summary>
    private Result CheckConditions(
        Guid changedFieldId,
        string changedKey,
        DocumentFieldDataType changedType,
        DocumentFieldSettings settings)
    {
        var nodes = _fields
            .Where(field => field.DocumentFieldId != changedFieldId)
            .Select(field => new ConditionNode(field.FieldKey, field.DataType, field.Behavior, field.Options))
            .Append(new ConditionNode(changedKey, changedType, settings.Behavior, settings.Options))
            .ToDictionary(node => node.Key, StringComparer.Ordinal);

        foreach (var node in nodes.Values.Where(node => node.Behavior.VisibleWhenFieldKey is not null))
        {
            if (node.Behavior.VisibleWhenFieldKey == node.Key
                || !nodes.TryGetValue(node.Behavior.VisibleWhenFieldKey!, out var controller)
                || controller.DataType is not (DocumentFieldDataType.Boolean or DocumentFieldDataType.Select or DocumentFieldDataType.MultiSelect))
            {
                return Result.Failure(DomainErrors.DocumentFields.ConditionFieldInvalid);
            }

            var value = node.Behavior.VisibleWhenValue!;
            var valid = controller.DataType == DocumentFieldDataType.Boolean
                ? value is "true" or "false"
                : controller.Behavior.OptionSource != DocumentFieldOptionSource.Static
                    || controller.Options.Any(option => option.Key == value);
            if (!valid)
                return Result.Failure(DomainErrors.DocumentFields.ConditionValueInvalid);

            var visited = new HashSet<string>(StringComparer.Ordinal) { node.Key };
            var cursor = controller;
            while (true)
            {
                if (!visited.Add(cursor.Key))
                    return Result.Failure(DomainErrors.DocumentFields.ConditionCycle);
                if (cursor.Behavior.VisibleWhenFieldKey is not { } next || !nodes.TryGetValue(next, out var parent))
                    break;
                cursor = parent;
            }
        }

        return Result.Success();
    }

    private Result EnsureDraft() =>
        Status == DocumentTypeSchemaVersionStatus.Draft
            ? Result.Success()
            : Result.Failure(DomainErrors.DocumentFields.SchemaVersionNotDraft);
}
