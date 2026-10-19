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

    private readonly List<DocumentParticipant> _participants = new();
    private readonly List<DocumentStatusHistory> _statusHistory = new();

    public Guid DocumentId
    {
        get => Id;
        private set => Id = value;
    }
    public string Title { get; private set; } = null!;
    public string? SubjectSummary { get; private set; }
    // FK → DocumentType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentType)
    public int DocumentTypeId { get; private set; }
    // FK → DocumentDirection.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentDirection)
    public int DirectionId { get; private set; }
    // FK → DocumentConfidentialityLevel.Id
    public int ConfidentialityLevelId { get; private set; } = DocumentConfidentialityLevel.NormalId;
    // FK → DocumentPriorityLevel.Id
    public int PriorityLevelId { get; private set; } = DocumentPriorityLevel.NormalId;
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
    public DateTime? DocumentDate { get; private set; }
    public DateTime? RegistrationDate { get; private set; }
    public string? ExternalReferenceNumber { get; private set; }
    public DateTime? ExternalDocumentDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public IReadOnlyCollection<DocumentParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<DocumentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Document()
    {
    }

    public static Result<Document> CreateDraft(
        string? title,
        int documentTypeId,
        int directionId,
        Guid owningDepartmentId,
        Guid createdByUserId,
        string? subjectSummary = null,
        int confidentialityLevelId = DocumentConfidentialityLevel.NormalId,
        int priorityLevelId = DocumentPriorityLevel.NormalId,
        DateTime? documentDate = null,
        string? externalReferenceNumber = null,
        DateTime? externalDocumentDate = null)
    {
        var titleResult = DocumentTitle.Create(title);
        if (titleResult.IsFailure)
            return Result.Failure<Document>(titleResult.Error);

        if (documentTypeId <= 0)
            return Result.Failure<Document>(DomainErrors.Correspondence.TypeCodeEmpty);

        if (directionId is not (DocumentDirection.InternalId or DocumentDirection.IncomingId or DocumentDirection.OutgoingId))
            return Result.Failure<Document>(DomainErrors.Correspondence.DirectionInvalid);

        var doc = new Document
        {
            DocumentId = Guid.NewGuid(),
            Title = titleResult.Value.Value,
            SubjectSummary = string.IsNullOrWhiteSpace(subjectSummary) ? null : subjectSummary.Trim(),
            DocumentTypeId = documentTypeId,
            DirectionId = directionId,
            ConfidentialityLevelId = confidentialityLevelId <= 0 ? DocumentConfidentialityLevel.NormalId : confidentialityLevelId,
            PriorityLevelId = priorityLevelId <= 0 ? DocumentPriorityLevel.NormalId : priorityLevelId,
            CurrentStatusId = DocumentStatusType.DraftId,
            OwningDepartmentId = owningDepartmentId,
            CreatedByUserId = createdByUserId,
            DocumentDate = documentDate ?? DateTime.UtcNow,
            ExternalReferenceNumber = externalReferenceNumber,
            ExternalDocumentDate = externalDocumentDate,
            CreatedAt = DateTime.UtcNow
        };

        doc.AppendStatusHistory(null, DocumentStatusType.DraftId, createdByUserId, "Initial draft created");
        doc.RaiseDomainEvent(new DocumentCreatedEvent(doc.DocumentId, doc.DocumentTypeId, doc.DirectionId, doc.OwningDepartmentId, doc.CreatedByUserId));

        return Result.Success(doc);
    }

    public Result UpdateDraftMetadata(
        string? title,
        string? subjectSummary,
        int? confidentialityLevelId = null,
        int? priorityLevelId = null)
    {
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

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result<DocumentParticipant> AddParticipant(
        int roleId,
        ParticipantTarget target,
        bool isPrimary = false,
        string? notes = null)
    {
        if (target.Kind == ParticipantTargetKind.External &&
            _participants.Any(p => p.ExternalOrganizationId.HasValue))
        {
            return Result.Failure<DocumentParticipant>(DomainErrors.Correspondence.SingleExternalPartyLimit);
        }

        var participantResult = DocumentParticipant.Create(DocumentId, roleId, target, isPrimary, notes);
        if (participantResult.IsFailure)
            return participantResult;

        _participants.Add(participantResult.Value);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentParticipantAddedEvent(DocumentId, participantResult.Value.ParticipantId, participantResult.Value.RoleId));
        return participantResult;
    }

    public Result Register(
        Guid officialNumberId,
        OfficialNumberValue officialNumberText,
        Guid actorUserId,
        string? notes = null)
    {
        if (CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftRegisterable);

        var previous = CurrentStatusId;
        ActiveOfficialNumberId = officialNumberId;
        OfficialNumberText = officialNumberText.Value;
        RegistrationDate = DateTime.UtcNow;
        CurrentStatusId = DocumentStatusType.RegisteredId;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, DocumentStatusType.RegisteredId, actorUserId, notes ?? $"Registered with number {officialNumberText.Value}");
        RaiseDomainEvent(new DocumentRegisteredEvent(DocumentId, officialNumberId, officialNumberText.Value, RegistrationDate.Value));
        return Result.Success();
    }

    /// <summary>
    /// Points the document at the official number that replaces its current one after a correction.
    /// The replaced number's own history keeps the link back; status is unchanged, because a number
    /// correction is not a lifecycle transition.
    /// </summary>
    public Result AttachReplacementNumber(Guid replacementNumberId, OfficialNumberValue replacementText)
    {
        if (ActiveOfficialNumberId is null)
            return Result.Failure(DomainErrors.Numbering.NumberNotFound);

        if (replacementNumberId == Guid.Empty || replacementNumberId == ActiveOfficialNumberId)
            return Result.Failure(DomainErrors.Numbering.ReplacementIdInvalid);

        ActiveOfficialNumberId = replacementNumberId;
        OfficialNumberText = replacementText.Value;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkInMovement(Guid actorUserId, string? notes = null)
    {
        if (CurrentStatusId is DocumentStatusType.ArchivedId or DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusId == DocumentStatusType.InMovementId)
            return Result.Success();

        var previous = CurrentStatusId;
        CurrentStatusId = DocumentStatusType.InMovementId;
        UpdatedAt = DateTime.UtcNow;
        AppendStatusHistory(previous, DocumentStatusType.InMovementId, actorUserId, notes);
        return Result.Success();
    }

    public Result Archive(Guid actorUserId, bool hasActiveMovements, string? reason = null)
    {
        if (CurrentStatusId == DocumentStatusType.ArchivedId)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusId == DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Correspondence.CannotArchiveCanceled);

        if (hasActiveMovements)
            return Result.Failure(DomainErrors.Correspondence.HasActiveMovements);

        var previous = CurrentStatusId;
        StatusBeforeArchiveId = previous;
        CurrentStatusId = DocumentStatusType.ArchivedId;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, DocumentStatusType.ArchivedId, actorUserId, reason);
        RaiseDomainEvent(new DocumentArchivedEvent(DocumentId, previous, actorUserId, reason));
        return Result.Success();
    }

    public Result Unarchive(Guid actorUserId, string? reason = null)
    {
        if (CurrentStatusId != DocumentStatusType.ArchivedId)
            return Result.Failure(DomainErrors.Correspondence.NotArchived);

        var restored = StatusBeforeArchiveId ?? DocumentStatusType.RegisteredId;
        StatusBeforeArchiveId = null;
        CurrentStatusId = restored;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(DocumentStatusType.ArchivedId, restored, actorUserId, reason);
        RaiseDomainEvent(new DocumentUnarchivedEvent(DocumentId, restored, actorUserId, reason));
        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, bool hasActiveMovements, string? reason)
    {
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
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, DocumentStatusType.CanceledId, actorUserId, reason.Trim());
        RaiseDomainEvent(new DocumentCanceledEvent(DocumentId, previous, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private void AppendStatusHistory(int? fromStatusId, int toStatusId, Guid changedByUserId, string? reason)
    {
        _statusHistory.Add(DocumentStatusHistory.Create(DocumentId, fromStatusId, toStatusId, changedByUserId, reason));
    }
}
