using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

public sealed class DocumentNumber : AggregateRoot<Guid>
{
    private readonly List<DocumentNumberHistory> _history = new();
    private readonly List<DocumentRegistryVisit> _visits = new();

    public Guid DocumentNumberId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → NumberingRegistry.RegistryId (InstitutionalDiwan.Domain.Entities.Numbering.NumberingRegistry)
    public Guid RegistryId { get; private set; }
    // FK → DocumentNumberScope.Id (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumberScope)
    public int ScopeId { get; private set; } = DocumentNumberScope.CentralId;
    // FK → Department.DepartmentId — register owner; null for a central Diwan number
    public Guid? DepartmentId { get; private set; }
    public int NumberingYear { get; private set; }
    public long SequenceValue { get; private set; }
    public string OfficialNumberText { get; private set; } = null!;
    // FK → DocumentNumberStatus.Id (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumberStatus)
    public int StatusId { get; private set; } = DocumentNumberStatus.ActiveId;
    // FK → User.Id (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid IssuedByUserId { get; private set; }
    public DateTime IssuedAt { get; private set; }
    // FK → DocumentNumber.DocumentNumberId (self-reference, the replacement number)
    public Guid? ReplacedByNumberId { get; private set; }
    public string? CancellationReason { get; private set; }
    /// <summary>
    /// Date written in the owning department's register (the latest one when the document reaches
    /// that department again). Null for a central Diwan number, which uses <see cref="IssuedAt"/>.
    /// </summary>
    public DateTime? RegistryDate { get; private set; }

    public IReadOnlyCollection<DocumentNumberHistory> History => _history.AsReadOnly();

    /// <summary>Every dated arrival recorded on this department line. Empty for a central number.</summary>
    public IReadOnlyCollection<DocumentRegistryVisit> Visits => _visits.AsReadOnly();

    public bool IsDepartmentEntry => ScopeId == DocumentNumberScope.DepartmentId;

    private DocumentNumber()
    {
    }

    public static Result<DocumentNumber> Issue(
        Guid documentId,
        Guid registryId,
        int numberingYear,
        long sequenceValue,
        string? officialNumberText,
        Guid issuedByUserId,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (numberingYear is < 1900 or > 2200)
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.YearInvalid);

        if (sequenceValue <= 0)
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.SequenceInvalid);

        var numberValResult = OfficialNumberValue.Create(officialNumberText);
        if (numberValResult.IsFailure)
            return Result.Failure<DocumentNumber>(numberValResult.Error);

        var docNumber = new DocumentNumber
        {
            DocumentNumberId = Guid.NewGuid(),
            DocumentId = documentId,
            RegistryId = registryId,
            ScopeId = DocumentNumberScope.CentralId,
            NumberingYear = numberingYear,
            SequenceValue = sequenceValue,
            OfficialNumberText = numberValResult.Value.Value,
            StatusId = DocumentNumberStatus.ActiveId,
            IssuedByUserId = issuedByUserId,
            IssuedAt = now
        };

        docNumber._history.Add(DocumentNumberHistory.Create(docNumber.DocumentNumberId, DocumentNumberEventType.IssuedId, issuedByUserId, notes, utcNow: now));
        docNumber.RegistryDate = docNumber.IssuedAt;
        docNumber.RaiseDomainEvent(new OfficialNumberIssuedEvent(
            docNumber.DocumentNumberId,
            docNumber.DocumentId,
            docNumber.RegistryId,
            docNumber.OfficialNumberText,
            docNumber.NumberingYear,
            docNumber.SequenceValue));

        return Result.Success(docNumber);
    }

    /// <summary>
    /// Creates the department-register line for a document that reached that department. The clerk
    /// types the number, so no sequence is allocated and the registry template is not applied; the
    /// numbering year is the year of the date written in the register.
    /// </summary>
    public static Result<DocumentNumber> CreateDepartmentEntry(
        Guid documentId,
        Guid departmentId,
        Guid registryId,
        string? registryNumberText,
        DateTime registryDate,
        Guid recordedByUserId,
        string? notes = null,
        DateTime? utcNow = null,
        Guid? movementStepId = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (departmentId == Guid.Empty)
            return Result.Failure<DocumentNumber>(DomainErrors.Department.NotFound);

        if (!IsValidRegistryDate(registryDate))
            return Result.Failure<DocumentNumber>(DomainErrors.Numbering.RegistryDateInvalid);

        var numberValResult = OfficialNumberValue.Create(registryNumberText);
        if (numberValResult.IsFailure)
            return Result.Failure<DocumentNumber>(numberValResult.Error);

        var entry = new DocumentNumber
        {
            DocumentNumberId = Guid.NewGuid(),
            DocumentId = documentId,
            RegistryId = registryId,
            ScopeId = DocumentNumberScope.DepartmentId,
            DepartmentId = departmentId,
            NumberingYear = registryDate.Year,
            SequenceValue = 0,
            OfficialNumberText = numberValResult.Value.Value,
            StatusId = DocumentNumberStatus.ActiveId,
            IssuedByUserId = recordedByUserId,
            IssuedAt = now,
            RegistryDate = registryDate
        };

        entry._history.Add(DocumentNumberHistory.Create(
            entry.DocumentNumberId,
            DocumentNumberEventType.RegisteredInDepartmentId,
            recordedByUserId,
            notes,
            registryDate,
            now));

        entry._visits.Add(DocumentRegistryVisit.Create(
            entry.DocumentNumberId, registryDate, recordedByUserId, movementStepId, notes, now));

        return Result.Success(entry);
    }

    /// <summary>
    /// Records another arrival in a department that already registered this document in this year.
    /// The number stays the same; a new dated visit is added. The same date cannot be used twice.
    /// </summary>
    public Result<DocumentRegistryVisit> RecordVisit(
        Guid actorUserId,
        DateTime registryDate,
        Guid? movementStepId = null,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var guard = EnsureCanAddVisit(registryDate);
        if (guard.IsFailure)
            return Result.Failure<DocumentRegistryVisit>(guard.Error);

        if (FindVisit(registryDate) is not null)
            return Result.Failure<DocumentRegistryVisit>(DomainErrors.Numbering.VisitDateDuplicate);

        return Result.Success(AddVisit(actorUserId, registryDate, movementStepId, notes, utcNow ?? DateTime.UtcNow));
    }

    /// <summary>
    /// Records the visit for a receipt. A visit on the same date that the clerk wrote by hand while
    /// this step was on its way (at or after <paramref name="stepSentAt"/>) and that no receipt has
    /// used yet is the same arrival, so it is linked to the step. Otherwise a new visit is added, even
    /// on a date the line already has: a document that leaves and comes back on the same day arrives
    /// twice, each arrival tied to its own movement step.
    /// </summary>
    public Result<DocumentRegistryVisit> RecordReceiptVisit(
        Guid actorUserId,
        DateTime registryDate,
        Guid movementStepId,
        DateTime? stepSentAt,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var guard = EnsureCanAddVisit(registryDate);
        if (guard.IsFailure)
            return Result.Failure<DocumentRegistryVisit>(guard.Error);

        if (_visits.Any(visit => visit.MovementStepId == movementStepId))
            return Result.Failure<DocumentRegistryVisit>(DomainErrors.Numbering.VisitDateDuplicate);

        var writtenForThisStep = _visits.FirstOrDefault(visit =>
            visit.RegistryDate == registryDate.Date
            && visit.MovementStepId is null
            && stepSentAt is { } sentAt
            && visit.RecordedAt >= sentAt);
        if (writtenForThisStep is null)
            return Result.Success(AddVisit(actorUserId, registryDate, movementStepId, notes, utcNow ?? DateTime.UtcNow));

        writtenForThisStep.LinkToStep(movementStepId);
        return Result.Success(writtenForThisStep);
    }

    /// <summary>
    /// Records the visit of a document handed back to this department without a receipt (a movement
    /// returned to its sender). It is tied to the returned step, so it may share a date with an
    /// earlier arrival.
    /// </summary>
    public Result<DocumentRegistryVisit> RecordReturnVisit(
        Guid actorUserId,
        DateTime registryDate,
        Guid movementStepId,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var guard = EnsureCanAddVisit(registryDate);
        if (guard.IsFailure)
            return Result.Failure<DocumentRegistryVisit>(guard.Error);

        if (_visits.Any(visit => visit.MovementStepId == movementStepId))
            return Result.Failure<DocumentRegistryVisit>(DomainErrors.Numbering.VisitDateDuplicate);

        return Result.Success(AddVisit(actorUserId, registryDate, movementStepId, notes, utcNow ?? DateTime.UtcNow));
    }

    private DocumentRegistryVisit? FindVisit(DateTime registryDate) =>
        _visits.FirstOrDefault(visit => visit.RegistryDate == registryDate.Date);

    private Result EnsureCanAddVisit(DateTime registryDate)
    {
        if (!IsDepartmentEntry)
            return Result.Failure(DomainErrors.Numbering.DepartmentScopeMismatch);

        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.NumberNotActive);

        if (!IsValidRegistryDate(registryDate))
            return Result.Failure(DomainErrors.Numbering.RegistryDateInvalid);

        return registryDate.Year == NumberingYear
            ? Result.Success()
            : Result.Failure(DomainErrors.Numbering.VisitYearMismatch);
    }

    private DocumentRegistryVisit AddVisit(
        Guid actorUserId, DateTime registryDate, Guid? movementStepId, string? notes, DateTime now)
    {
        var visit = DocumentRegistryVisit.Create(
            DocumentNumberId, registryDate, actorUserId, movementStepId, notes, now);
        _visits.Add(visit);

        // RegistryDate is the latest arrival, so a back-dated visit must not move it backwards.
        if (RegistryDate is null || registryDate >= RegistryDate)
            RegistryDate = registryDate;

        _history.Add(DocumentNumberHistory.Create(
            DocumentNumberId,
            DocumentNumberEventType.ReRegisteredInDepartmentId,
            actorUserId,
            notes,
            registryDate,
            now));

        return visit;
    }

    public Result Cancel(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.AlreadyCancelled);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Numbering.CancelReasonRequired);

        StatusId = DocumentNumberStatus.CancelledId;
        CancellationReason = reason.Trim();
        _history.Add(DocumentNumberHistory.Create(DocumentNumberId, DocumentNumberEventType.CancelledId, actorUserId, reason.Trim(), utcNow: utcNow));

        RaiseDomainEvent(new OfficialNumberCancelledEvent(DocumentNumberId, DocumentId, OfficialNumberText, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result ReplaceWith(Guid replacementNumberId, Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        if (IsDepartmentEntry)
            return Result.Failure(DomainErrors.Numbering.DepartmentScopeMismatch);

        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.AlreadyCancelled);

        if (replacementNumberId == Guid.Empty || replacementNumberId == DocumentNumberId)
            return Result.Failure(DomainErrors.Numbering.ReplacementIdInvalid);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Numbering.CancelReasonRequired);

        StatusId = DocumentNumberStatus.ReplacedId;
        ReplacedByNumberId = replacementNumberId;
        CancellationReason = reason.Trim();
        _history.Add(DocumentNumberHistory.Create(DocumentNumberId, DocumentNumberEventType.ReplacedId, actorUserId, reason.Trim(), utcNow: utcNow));

        RaiseDomainEvent(new OfficialNumberReplacedEvent(DocumentNumberId, replacementNumberId, DocumentId, actorUserId, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// The department wrote the document under a new number when it came back. This line stays in the
    /// register book as <c>REPLACED</c> and points at the line that took its place, so either number
    /// leads to the other. Unlike a central replacement no reason is required: it is an ordinary
    /// decision of the receiving clerk.
    /// </summary>
    public Result SupersedeInDepartment(
        DocumentNumber replacement,
        Guid actorUserId,
        string? notes = null,
        DateTime? utcNow = null)
    {
        if (!IsDepartmentEntry || !replacement.IsDepartmentEntry)
            return Result.Failure(DomainErrors.Numbering.DepartmentScopeMismatch);

        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.NumberNotActive);

        if (replacement.DocumentNumberId == DocumentNumberId
            || replacement.DocumentId != DocumentId
            || replacement.DepartmentId != DepartmentId)
        {
            return Result.Failure(DomainErrors.Numbering.ReplacementIdInvalid);
        }

        var reason = string.IsNullOrWhiteSpace(notes)
            ? $"Replaced by department number {replacement.OfficialNumberText}"
            : notes.Trim();

        StatusId = DocumentNumberStatus.ReplacedId;
        ReplacedByNumberId = replacement.DocumentNumberId;
        _history.Add(DocumentNumberHistory.Create(
            DocumentNumberId, DocumentNumberEventType.ReplacedId, actorUserId, reason, utcNow: utcNow));

        RaiseDomainEvent(new OfficialNumberReplacedEvent(
            DocumentNumberId, replacement.DocumentNumberId, DocumentId, actorUserId, reason));
        return Result.Success();
    }

    private static bool IsValidRegistryDate(DateTime registryDate) =>
        registryDate != default && registryDate.Year is >= 1900 and <= 2200;
}
