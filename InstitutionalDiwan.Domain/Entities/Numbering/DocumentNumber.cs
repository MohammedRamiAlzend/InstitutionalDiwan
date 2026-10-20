using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

public sealed class DocumentNumber : AggregateRoot<Guid>
{
    private readonly List<DocumentNumberHistory> _history = new();

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
        string? notes = null)
    {
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
            IssuedAt = DateTime.UtcNow
        };

        docNumber._history.Add(DocumentNumberHistory.Create(docNumber.DocumentNumberId, DocumentNumberEventType.IssuedId, issuedByUserId, notes));
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
        string? notes = null)
    {
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
            IssuedAt = DateTime.UtcNow,
            RegistryDate = registryDate
        };

        entry._history.Add(DocumentNumberHistory.Create(
            entry.DocumentNumberId,
            DocumentNumberEventType.RegisteredInDepartmentId,
            recordedByUserId,
            notes,
            registryDate));

        return Result.Success(entry);
    }

    /// <summary>
    /// Records another arrival in a department that already registered this document. The number
    /// stays the same — only a new date and a history line are added.
    /// </summary>
    public Result ReRegisterInDepartment(Guid actorUserId, DateTime registryDate, string? notes = null)
    {
        if (!IsDepartmentEntry)
            return Result.Failure(DomainErrors.Numbering.DepartmentScopeMismatch);

        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.NumberNotActive);

        if (!IsValidRegistryDate(registryDate))
            return Result.Failure(DomainErrors.Numbering.RegistryDateInvalid);

        RegistryDate = registryDate;
        _history.Add(DocumentNumberHistory.Create(
            DocumentNumberId,
            DocumentNumberEventType.ReRegisteredInDepartmentId,
            actorUserId,
            notes,
            registryDate));

        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, string? reason)
    {
        if (StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure(DomainErrors.Numbering.AlreadyCancelled);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Numbering.CancelReasonRequired);

        StatusId = DocumentNumberStatus.CancelledId;
        CancellationReason = reason.Trim();
        _history.Add(DocumentNumberHistory.Create(DocumentNumberId, DocumentNumberEventType.CancelledId, actorUserId, reason.Trim()));

        RaiseDomainEvent(new OfficialNumberCancelledEvent(DocumentNumberId, DocumentId, OfficialNumberText, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result ReplaceWith(Guid replacementNumberId, Guid actorUserId, string? reason)
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
        _history.Add(DocumentNumberHistory.Create(DocumentNumberId, DocumentNumberEventType.ReplacedId, actorUserId, reason.Trim()));

        RaiseDomainEvent(new OfficialNumberReplacedEvent(DocumentNumberId, replacementNumberId, DocumentId, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private static bool IsValidRegistryDate(DateTime registryDate) =>
        registryDate != default && registryDate.Year is >= 1900 and <= 2200;
}
