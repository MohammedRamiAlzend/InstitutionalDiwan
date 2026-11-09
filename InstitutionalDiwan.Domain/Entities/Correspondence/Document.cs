using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class Document : AggregateRoot<Guid>
{
    public const int StatusDraft = DocumentStatusType.DraftId;
    public const int StatusRegistered = DocumentStatusType.RegisteredId;
    public const int StatusInMovement = DocumentStatusType.InMovementId;
    public const int StatusArchived = DocumentStatusType.ArchivedId;
    public const int StatusCanceled = DocumentStatusType.CanceledId;
    public const int MaxDisplayNameLength = 300;

    private const string DisplayNameSeparator = " · ";

    private readonly List<DocumentParticipant> _participants = new();
    private readonly List<DocumentPerson> _persons = new();
    private readonly List<DocumentStatusHistory> _statusHistory = new();
    private readonly List<DocumentNumber> _numbers = new();
    private readonly List<DocumentFieldValue> _fieldValues = new();

    public Guid DocumentId
    {
        get => Id;
        private set => Id = value;
    }
    /// <summary>No longer asked for or shown; kept so the text of older documents is not lost.</summary>
    public string? Title { get; private set; }

    /// <summary>No longer asked for or shown; kept so the text of older documents is not lost.</summary>
    public string? SubjectSummary { get; private set; }

    /// <summary>
    /// What the document is called everywhere: its type and its number in the owning department's
    /// register, or its type and creation date until it has one. Written by the system.
    /// </summary>
    public string DisplayName { get; private set; } = null!;
    // FK → DocumentType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentType)
    public int DocumentTypeId { get; private set; }
    // FK → DocumentDirection.Id. Suspended for now: null means "not classified" and is the default.
    public int? DirectionId { get; private set; }
    // FK → DocumentConfidentialityLevel.Id. Suspended for now: null is treated as normal. Values already
    // stored (e.g. SECRET) keep restricting access.
    public int? ConfidentialityLevelId { get; private set; }
    // FK → DocumentPriorityLevel.Id. Suspended for now: null means "not set".
    public int? PriorityLevelId { get; private set; }
    // FK → DocumentStatusType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public int CurrentStatusId { get; private set; } = DocumentStatusType.DraftId;
    public int? StatusBeforeArchiveId { get; private set; }
    // FK → Department.Id (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid OwningDepartmentId { get; private set; }
    // FK → User.Id (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    // FK → DocumentNumber.DocumentNumberId (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumber)
    public Guid? ActiveOfficialNumberId { get; private set; }
    public string? OfficialNumberText { get; private set; }
    public DateTime? RegistrationDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    /// <summary>
    /// FK → DocumentTypeSchemaVersion.SchemaVersionId. The dynamic-field schema this document was pinned
    /// to when it was created; null when its type had no published schema. Never changes afterwards.
    /// </summary>
    public Guid? SchemaVersionId { get; private set; }

    /// <summary>The stored dynamic-field values, validated against <see cref="SchemaVersionId"/>'s schema.</summary>
    public IReadOnlyCollection<DocumentFieldValue> FieldValues => _fieldValues.AsReadOnly();

    public IReadOnlyCollection<DocumentParticipant> Participants => _participants.AsReadOnly();

    /// <summary>The people named on this document (defendants, plaintiffs, ...), who are not system users.</summary>
    public IReadOnlyCollection<DocumentPerson> Persons => _persons.AsReadOnly();

    public IReadOnlyCollection<DocumentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    /// <summary>
    /// Every number this document carries: the central Diwan number plus at most one line per
    /// department register it was written into.
    /// </summary>
    public IReadOnlyCollection<DocumentNumber> Numbers => _numbers.AsReadOnly();

    private Document()
    {
    }

    public static Result<Document> CreateDraft(
        string? title,
        int documentTypeId,
        int? directionId,
        Guid owningDepartmentId,
        Guid createdByUserId,
        string? subjectSummary = null,
        int? confidentialityLevelId = null,
        int? priorityLevelId = null,
        DateTime? utcNow = null,
        Guid? schemaVersionId = null,
        string? displayName = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        string? cleanTitle = null;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleResult = DocumentTitle.Create(title);
            if (titleResult.IsFailure)
                return Result.Failure<Document>(titleResult.Error);

            cleanTitle = titleResult.Value.Value;
        }

        var name = string.IsNullOrWhiteSpace(displayName) ? cleanTitle : displayName.Trim();
        if (name is null)
            return Result.Failure<Document>(DomainErrors.Correspondence.DisplayNameEmpty);

        if (documentTypeId <= 0)
            return Result.Failure<Document>(DomainErrors.Correspondence.TypeCodeEmpty);

        if (directionId is { } direction &&
            direction is not (DocumentDirection.InternalId or DocumentDirection.IncomingId or DocumentDirection.OutgoingId))
            return Result.Failure<Document>(DomainErrors.Correspondence.DirectionInvalid);

        var doc = new Document
        {
            DocumentId = Guid.NewGuid(),
            Title = cleanTitle,
            DisplayName = name,
            SubjectSummary = string.IsNullOrWhiteSpace(subjectSummary) ? null : subjectSummary.Trim(),
            DocumentTypeId = documentTypeId,
            DirectionId = directionId,
            ConfidentialityLevelId = confidentialityLevelId is > 0 ? confidentialityLevelId : null,
            PriorityLevelId = priorityLevelId is > 0 ? priorityLevelId : null,
            CurrentStatusId = DocumentStatusType.DraftId,
            OwningDepartmentId = owningDepartmentId,
            CreatedByUserId = createdByUserId,
            SchemaVersionId = schemaVersionId,
            CreatedAt = now
        };

        doc.AppendStatusHistory(null, DocumentStatusType.DraftId, createdByUserId, "Initial draft created", now);
        doc.RaiseDomainEvent(new DocumentCreatedEvent(doc.DocumentId, doc.DocumentTypeId, doc.DirectionId, doc.OwningDepartmentId, doc.CreatedByUserId));

        return Result.Success(doc);
    }

    /// <summary>
    /// Replaces the dynamic-field values of a draft. <paramref name="schemaVersion"/> must be the version
    /// this document is pinned to; values are validated against it, and blank values clear a field.
    /// </summary>
    public Result SetFieldValues(
        DocumentTypeSchemaVersion schemaVersion,
        IReadOnlyCollection<DocumentFieldValueInput> inputs,
        DateTime? utcNow = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null)
    {
        if (CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftEditable);

        if (SchemaVersionId is null || SchemaVersionId != schemaVersion.SchemaVersionId)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionMismatch);

        var built = schemaVersion.BuildValues(DocumentId, inputs, _fieldValues, sourcedOptions, utcNow);
        if (built.IsFailure)
            return built;

        _fieldValues.Clear();
        _fieldValues.AddRange(built.Value);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Writes the read-only values of the pinned schema that are still missing and leaves every other
    /// value alone. Covers a document created without any submitted field values.
    /// </summary>
    public Result AddMissingFixedFieldValues(DocumentTypeSchemaVersion schemaVersion, DateTime utcNow)
    {
        if (SchemaVersionId is null || SchemaVersionId != schemaVersion.SchemaVersionId)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionMismatch);

        var missing = schemaVersion.BuildMissingFixedValues(DocumentId, _fieldValues, utcNow);
        if (missing.IsFailure)
            return missing;

        _fieldValues.AddRange(missing.Value);
        return Result.Success();
    }

    /// <summary>
    /// Fails if a required dynamic field of the pinned schema has no value. Run before
    /// <see cref="Register"/>; a document without a pinned schema has nothing to check.
    /// </summary>
    public Result EnsureRequiredFieldsComplete(DocumentTypeSchemaVersion? schemaVersion)
    {
        if (SchemaVersionId is null)
            return Result.Success();

        if (schemaVersion is null || schemaVersion.SchemaVersionId != SchemaVersionId)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionMismatch);

        return schemaVersion.CheckRequiredFields(_fieldValues);
    }

    public Result UpdateDraftMetadata(
        string? title,
        string? subjectSummary,
        int? confidentialityLevelId = null,
        int? priorityLevelId = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftEditable);

        var titleResult = DocumentTitle.Create(title);
        if (titleResult.IsFailure)
            return titleResult;

        Title = titleResult.Value.Value;
        SubjectSummary = string.IsNullOrWhiteSpace(subjectSummary) ? null : subjectSummary.Trim();

        if (confidentialityLevelId is { } confidentiality && confidentiality > 0)
            ConfidentialityLevelId = confidentiality;

        if (priorityLevelId is { } priority && priority > 0)
            PriorityLevelId = priority;

        UpdatedAt = now;
        return Result.Success();
    }

    public Result<DocumentParticipant> AddParticipant(
        int roleId,
        ParticipantTarget target,
        bool isPrimary = false,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        var notArchived = EnsureNotArchived();
        if (notArchived.IsFailure)
            return Result.Failure<DocumentParticipant>(notArchived.Error);

        if (target.Kind == ParticipantTargetKind.External &&
            _participants.Any(p => p.ExternalOrganizationId.HasValue))
        {
            return Result.Failure<DocumentParticipant>(DomainErrors.Correspondence.SingleExternalPartyLimit);
        }

        var participantResult = DocumentParticipant.Create(DocumentId, roleId, target, isPrimary, notes, now);
        if (participantResult.IsFailure)
            return participantResult;

        _participants.Add(participantResult.Value);
        UpdatedAt = now;
        RaiseDomainEvent(new DocumentParticipantAddedEvent(DocumentId, participantResult.Value.ParticipantId, participantResult.Value.RoleId));
        return participantResult;
    }

    /// <summary>Names a person on this document in the given capacity.</summary>
    public Result<DocumentPerson> LinkPerson(Guid personId, int roleId, Guid actorUserId, DateTime? utcNow = null)
    {
        var editable = EnsurePersonsEditable();
        if (editable.IsFailure)
            return Result.Failure<DocumentPerson>(editable.Error);

        if (_persons.Any(link => link.PersonId == personId))
            return Result.Failure<DocumentPerson>(DomainErrors.Persons.DuplicateInDocument);

        var now = utcNow ?? DateTime.UtcNow;
        var link = DocumentPerson.Create(DocumentId, personId, roleId, actorUserId, now);

        _persons.Add(link);
        UpdatedAt = now;
        RaiseDomainEvent(new DocumentPersonLinkedEvent(DocumentId, link.DocumentPersonId, personId, roleId, actorUserId));
        return Result.Success(link);
    }

    /// <summary>Changes the capacity a person holds on this document.</summary>
    public Result<DocumentPerson> ChangePersonRole(Guid documentPersonId, int roleId, Guid actorUserId, DateTime? utcNow = null)
    {
        var editable = EnsurePersonsEditable();
        if (editable.IsFailure)
            return Result.Failure<DocumentPerson>(editable.Error);

        var link = _persons.FirstOrDefault(candidate => candidate.DocumentPersonId == documentPersonId);
        if (link is null)
            return Result.Failure<DocumentPerson>(DomainErrors.Persons.LinkNotFound);

        var previousRoleId = link.RoleId;
        if (previousRoleId == roleId)
            return Result.Success(link);

        link.ChangeRole(roleId);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        RaiseDomainEvent(new DocumentPersonRoleChangedEvent(DocumentId, documentPersonId, link.PersonId, previousRoleId, roleId, actorUserId));
        return Result.Success(link);
    }

    /// <summary>Takes a person off this document. The person's own record stays.</summary>
    public Result UnlinkPerson(Guid documentPersonId, Guid actorUserId, DateTime? utcNow = null)
    {
        var editable = EnsurePersonsEditable();
        if (editable.IsFailure)
            return editable;

        var link = _persons.FirstOrDefault(candidate => candidate.DocumentPersonId == documentPersonId);
        if (link is null)
            return Result.Failure(DomainErrors.Persons.LinkNotFound);

        _persons.Remove(link);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        RaiseDomainEvent(new DocumentPersonUnlinkedEvent(DocumentId, documentPersonId, link.PersonId, link.RoleId, actorUserId));
        return Result.Success();
    }

    private Result EnsurePersonsEditable()
    {
        var notArchived = EnsureNotArchived();
        if (notArchived.IsFailure)
            return notArchived;

        return CurrentStatusId == DocumentStatusType.CanceledId
            ? Result.Failure(DomainErrors.Persons.DocumentCanceled)
            : Result.Success();
    }

    public Result Register(
        Guid officialNumberId,
        OfficialNumberValue officialNumberText,
        Guid actorUserId,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftRegisterable);

        var previous = CurrentStatusId;
        ActiveOfficialNumberId = officialNumberId;
        OfficialNumberText = officialNumberText.Value;
        RegistrationDate = now;
        CurrentStatusId = DocumentStatusType.RegisteredId;
        UpdatedAt = now;

        AppendStatusHistory(previous, DocumentStatusType.RegisteredId, actorUserId, notes ?? $"Registered with number {officialNumberText.Value}", now);
        RaiseDomainEvent(new DocumentRegisteredEvent(DocumentId, officialNumberId, officialNumberText.Value, RegistrationDate.Value));
        return Result.Success();
    }

    /// <summary>
    /// Points the document at the official number that replaces its current one after a correction.
    /// The replaced number's own history keeps the link back; status is unchanged, because a number
    /// correction is not a lifecycle transition.
    /// </summary>
    public Result AttachReplacementNumber(Guid replacementNumberId, OfficialNumberValue replacementText, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var notArchived = EnsureNotArchived();
        if (notArchived.IsFailure)
            return notArchived;

        if (ActiveOfficialNumberId is null)
            return Result.Failure(DomainErrors.Numbering.NumberNotFound);

        if (replacementNumberId == Guid.Empty || replacementNumberId == ActiveOfficialNumberId)
            return Result.Failure(DomainErrors.Numbering.ReplacementIdInvalid);

        ActiveOfficialNumberId = replacementNumberId;
        OfficialNumberText = replacementText.Value;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>The name of a document of this type: with its register number, or its date until it has one.</summary>
    public static string BuildDisplayName(string typeLabel, string? registerNumber, DateTime createdAtUtc) =>
        $"{typeLabel}{DisplayNameSeparator}{(string.IsNullOrWhiteSpace(registerNumber) ? createdAtUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : registerNumber.Trim())}";

    /// <summary>
    /// Names the document after its first line in the owning department's register. Later lines (a
    /// new year, another department) leave the name alone, so a document keeps the name it is known by.
    /// </summary>
    private void TakeNameFromFirstOwnRegisterLine(DocumentNumber number)
    {
        if (number.DepartmentId != OwningDepartmentId)
            return;

        if (_numbers.Count(existing => existing.IsDepartmentEntry && existing.DepartmentId == OwningDepartmentId) != 1)
            return;

        var separatorIndex = DisplayName.IndexOf(DisplayNameSeparator, StringComparison.Ordinal);
        if (separatorIndex >= 0)
            DisplayName = $"{DisplayName[..separatorIndex]}{DisplayNameSeparator}{number.OfficialNumberText}";
    }

    /// <summary>The department-register line for <paramref name="departmentId"/> in <paramref name="year"/>, if any.</summary>
    public DocumentNumber? FindDepartmentNumber(Guid departmentId, int year) =>
        _numbers.FirstOrDefault(number =>
            number.IsDepartmentEntry
            && number.DepartmentId == departmentId
            && number.NumberingYear == year);

    /// <summary>
    /// Attaches a department-register entry to this document. One line per department per year:
    /// later arrivals in that year reuse the number and only add a dated visit.
    /// </summary>
    public Result AddDepartmentNumber(DocumentNumber number, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (number is null)
            return Result.Failure(DomainErrors.Numbering.NumberNotFound);

        if (number.DocumentId != DocumentId)
            return Result.Failure(DomainErrors.Correspondence.DocumentNotFound);

        if (!number.IsDepartmentEntry || number.DepartmentId is null)
            return Result.Failure(DomainErrors.Numbering.DepartmentScopeMismatch);

        if (_numbers.Any(existing =>
                existing.IsDepartmentEntry
                && existing.DepartmentId == number.DepartmentId
                && existing.NumberingYear == number.NumberingYear))
        {
            return Result.Failure(DomainErrors.Numbering.DepartmentNumberLocked);
        }

        _numbers.Add(number);
        TakeNameFromFirstOwnRegisterLine(number);
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MarkInMovement(Guid actorUserId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (CurrentStatusId is DocumentStatusType.ArchivedId or DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusId == DocumentStatusType.InMovementId)
            return Result.Success();

        var previous = CurrentStatusId;
        CurrentStatusId = DocumentStatusType.InMovementId;
        UpdatedAt = now;
        AppendStatusHistory(previous, DocumentStatusType.InMovementId, actorUserId, notes, now);
        return Result.Success();
    }

    /// <summary>
    /// Ends the document's movement: the document leaves <c>IN_MOVEMENT</c> and returns to
    /// <c>REGISTERED</c>, the status it held when the movement started. Calling it on a document
    /// that is not in movement is a no-op, so closing a movement twice is harmless.
    /// </summary>
    public Result MarkMovementEnded(Guid actorUserId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (CurrentStatusId != DocumentStatusType.InMovementId)
            return Result.Success();

        var previous = CurrentStatusId;
        CurrentStatusId = DocumentStatusType.RegisteredId;
        UpdatedAt = now;

        AppendStatusHistory(previous, DocumentStatusType.RegisteredId, actorUserId, notes ?? "Movement ended", now);
        return Result.Success();
    }

    /// <summary>Fails when the document is archived; every write outside the archive lifecycle runs this first.</summary>
    public Result EnsureNotArchived() =>
        CurrentStatusId == DocumentStatusType.ArchivedId
            ? Result.Failure(DomainErrors.Correspondence.DocumentArchived)
            : Result.Success();

    /// <summary>
    /// Moves a registered document with no active movement into the archive at
    /// <paramref name="archiveLocationId"/>. The caller records the matching <c>DocumentArchiveEvent</c>.
    /// </summary>
    public Result Archive(
        Guid actorUserId,
        bool hasActiveMovements,
        int archiveLocationId,
        string? reason = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (CurrentStatusId == DocumentStatusType.ArchivedId)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusId == DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Correspondence.CannotArchiveCanceled);

        if (hasActiveMovements)
            return Result.Failure(DomainErrors.Correspondence.HasActiveMovements);

        if (CurrentStatusId != DocumentStatusType.RegisteredId)
            return Result.Failure(DomainErrors.Correspondence.OnlyRegisteredArchivable);

        if (archiveLocationId <= 0)
            return Result.Failure(DomainErrors.Archiving.LocationNotFound);

        var previous = CurrentStatusId;
        StatusBeforeArchiveId = previous;
        CurrentStatusId = DocumentStatusType.ArchivedId;
        UpdatedAt = now;

        AppendStatusHistory(previous, DocumentStatusType.ArchivedId, actorUserId, reason, now);
        RaiseDomainEvent(new DocumentArchivedEvent(DocumentId, previous, actorUserId, reason, archiveLocationId));
        return Result.Success();
    }

    public Result Unarchive(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (CurrentStatusId != DocumentStatusType.ArchivedId)
            return Result.Failure(DomainErrors.Correspondence.NotArchived);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Archiving.UnarchiveReasonEmpty);

        reason = reason.Trim();
        var restored = StatusBeforeArchiveId ?? DocumentStatusType.RegisteredId;
        StatusBeforeArchiveId = null;
        CurrentStatusId = restored;
        UpdatedAt = now;

        AppendStatusHistory(DocumentStatusType.ArchivedId, restored, actorUserId, reason, now);
        RaiseDomainEvent(new DocumentUnarchivedEvent(DocumentId, restored, actorUserId, reason));
        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, bool hasActiveMovements, string? reason, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (CurrentStatusId == DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Correspondence.AlreadyCanceled);

        if (CurrentStatusId == DocumentStatusType.ArchivedId)
            return Result.Failure(DomainErrors.Correspondence.CannotCancelArchived);

        if (hasActiveMovements)
            return Result.Failure(DomainErrors.Correspondence.HasActiveMovements);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Correspondence.CancelReasonEmpty);

        var previous = CurrentStatusId;
        CurrentStatusId = DocumentStatusType.CanceledId;
        UpdatedAt = now;

        AppendStatusHistory(previous, DocumentStatusType.CanceledId, actorUserId, reason.Trim(), now);
        RaiseDomainEvent(new DocumentCanceledEvent(DocumentId, previous, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private void AppendStatusHistory(int? fromStatusId, int toStatusId, Guid changedByUserId, string? reason, DateTime utcNow)
    {
        _statusHistory.Add(DocumentStatusHistory.Create(DocumentId, fromStatusId, toStatusId, changedByUserId, reason, utcNow));
    }
}
