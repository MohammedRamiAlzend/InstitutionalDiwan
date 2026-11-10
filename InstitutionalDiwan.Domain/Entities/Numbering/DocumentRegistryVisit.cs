using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>
/// One arrival of a document in a department's register: the department's number stays on the
/// parent <see cref="DocumentNumber"/>, and each visit adds a date. A date written by hand can appear
/// only once per register line; arrivals tied to different movement steps may share a date.
/// <para>
/// A visit also carries the department's own record of what happened while it held the document:
/// values of the department's record form for the document type, pinned to the schema version that
/// was published when the record was opened. The record is editable while the document stays in the
/// department and is locked (<see cref="LockedAt"/>) the moment the document leaves; after that only
/// a correction with a reason can change it.
/// </para>
/// </summary>
public sealed class DocumentRegistryVisit : Entity<Guid>
{
    public const int MaxCorrectionReasonLength = 1000;

    private readonly List<DepartmentRecordValue> _values = new();

    public Guid VisitId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentNumber.DocumentNumberId (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumber)
    public Guid DocumentNumberId { get; private set; }
    /// <summary>Date written in the department register for this visit (no time part).</summary>
    public DateTime RegistryDate { get; private set; }
    // FK → DocumentMovementStep.StepId — null until a receipt claims this visit
    public Guid? MovementStepId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid RecordedByUserId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// FK → DocumentTypeSchemaVersion.SchemaVersionId. The department record form this visit was opened
    /// with; null when the department had no published form for the document type.
    /// </summary>
    public Guid? SchemaVersionId { get; private set; }
    /// <summary>When the document left the department; the record is read-only from then on.</summary>
    public DateTime? LockedAt { get; private set; }
    public DateTime? RecordUpdatedAt { get; private set; }
    // FK → User.UserId — who last changed the record values
    public Guid? RecordUpdatedByUserId { get; private set; }
    public DateTime? CorrectedAt { get; private set; }
    // FK → User.UserId — who made the last correction of the locked record
    public Guid? CorrectedByUserId { get; private set; }
    public string? CorrectionReason { get; private set; }

    public IReadOnlyCollection<DepartmentRecordValue> Values => _values.AsReadOnly();

    public bool IsLocked => LockedAt is not null;

    private DocumentRegistryVisit()
    {
    }

    internal static DocumentRegistryVisit Create(
        Guid documentNumberId,
        DateTime registryDate,
        Guid recordedByUserId,
        Guid? movementStepId,
        string? notes,
        DateTime recordedAt)
    {
        return new DocumentRegistryVisit
        {
            VisitId = Guid.NewGuid(),
            DocumentNumberId = documentNumberId,
            RegistryDate = registryDate.Date,
            MovementStepId = movementStepId,
            RecordedByUserId = recordedByUserId,
            RecordedAt = recordedAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    internal void LinkToStep(Guid movementStepId) => MovementStepId = movementStepId;

    /// <summary>The stored values as field values, for validation and display.</summary>
    public IReadOnlyList<DocumentFieldValue> FieldValues() =>
        _values.Select(value => value.AsFieldValue()).ToList();

    /// <summary>
    /// Opens the department record on this visit: pins it to <paramref name="schema"/> and stores the
    /// submitted values. With <paramref name="requireComplete"/> every required visible field must
    /// have a value (the receipt), otherwise only read-only fields get their fixed value (a document
    /// handed back without a receipt). Without a schema no value can belong anywhere.
    /// </summary>
    public Result OpenRecord(
        DocumentTypeSchemaVersion? schema,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        Guid actorUserId,
        DateTime utcNow,
        bool requireComplete,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null)
    {
        if (schema is null)
        {
            return inputs.Count == 0
                ? Result.Success()
                : Result.Failure(DomainErrors.DocumentFields.UnknownField(inputs.First().FieldKey));
        }

        if (IsLocked)
            return Result.Failure(DomainErrors.DepartmentRecords.VisitLocked);

        if (SchemaVersionId is not null && SchemaVersionId != schema.SchemaVersionId)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionMismatch);

        SchemaVersionId = schema.SchemaVersionId;
        return Apply(schema, inputs, actorUserId, utcNow, requireComplete, sourcedOptions);
    }

    /// <summary>Replaces the record values of an open visit; a blank value clears that field.</summary>
    public Result SetRecordValues(
        DocumentTypeSchemaVersion schema,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        Guid actorUserId,
        DateTime utcNow,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null)
    {
        if (IsLocked)
            return Result.Failure(DomainErrors.DepartmentRecords.VisitLocked);

        var pinned = EnsurePinnedTo(schema);
        return pinned.IsFailure
            ? pinned
            : Apply(schema, inputs, actorUserId, utcNow, requireComplete: true, sourcedOptions);
    }

    /// <summary>
    /// Corrects the values of a locked visit. Kept for administrators: the reason is stored on the
    /// visit and the change goes through the tamper-evident audit trail like every other write.
    /// </summary>
    public Result CorrectRecordValues(
        DocumentTypeSchemaVersion schema,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        string? reason,
        Guid actorUserId,
        DateTime utcNow,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null)
    {
        if (!IsLocked)
            return Result.Failure(DomainErrors.DepartmentRecords.VisitNotLocked);

        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result.Failure(DomainErrors.DepartmentRecords.CorrectionReasonRequired);

        if (trimmed.Length > MaxCorrectionReasonLength)
            return Result.Failure(DomainErrors.DepartmentRecords.CorrectionReasonTooLong);

        var pinned = EnsurePinnedTo(schema);
        if (pinned.IsFailure)
            return pinned;

        var applied = Apply(schema, inputs, actorUserId, utcNow, requireComplete: true, sourcedOptions);
        if (applied.IsFailure)
            return applied;

        CorrectedAt = utcNow;
        CorrectedByUserId = actorUserId;
        CorrectionReason = trimmed;
        return Result.Success();
    }

    /// <summary>Freezes the record because the document left the department. Locking twice is harmless.</summary>
    public void Lock(DateTime utcNow) => LockedAt ??= utcNow;

    private Result EnsurePinnedTo(DocumentTypeSchemaVersion schema)
    {
        if (SchemaVersionId is null)
            return Result.Failure(DomainErrors.DepartmentRecords.SchemaNotFound);

        return SchemaVersionId == schema.SchemaVersionId
            ? Result.Success()
            : Result.Failure(DomainErrors.DocumentFields.SchemaVersionMismatch);
    }

    private Result Apply(
        DocumentTypeSchemaVersion schema,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        Guid actorUserId,
        DateTime utcNow,
        bool requireComplete,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions)
    {
        var built = schema.BuildValues(VisitId, inputs, FieldValues(), sourcedOptions, utcNow);
        if (built.IsFailure)
            return built;

        if (requireComplete)
        {
            var complete = schema.CheckRequiredFields(built.Value);
            if (complete.IsFailure)
                return complete;
        }

        // Update in place so a field keeps its row (and its audit history) across edits.
        var byField = built.Value.ToDictionary(value => value.DocumentFieldId);
        _values.RemoveAll(stored => !byField.ContainsKey(stored.DocumentFieldId));
        foreach (var value in built.Value)
        {
            var stored = _values.FirstOrDefault(existing => existing.DocumentFieldId == value.DocumentFieldId);
            if (stored is null)
                _values.Add(DepartmentRecordValue.From(VisitId, value));
            else
                stored.Apply(value);
        }

        RecordUpdatedAt = utcNow;
        RecordUpdatedByUserId = actorUserId;
        return Result.Success();
    }
}
