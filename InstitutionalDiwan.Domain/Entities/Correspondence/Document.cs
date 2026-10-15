using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class Document : AggregateRoot<Guid>
{
    public const string StatusDraft = "DRAFT";
    public const string StatusRegistered = "REGISTERED";
    public const string StatusInMovement = "IN_MOVEMENT";
    public const string StatusArchived = "ARCHIVED";
    public const string StatusCanceled = "CANCELED";

    private readonly List<DocumentParticipant> _participants = new();
    private readonly List<DocumentStatusHistory> _statusHistory = new();

    public Guid DocumentId
    {
        get => Id;
        private set => Id = value;
    }

    public string Title { get; private set; } = null!;
    public string? SubjectSummary { get; private set; }
    public string DocumentTypeCode { get; private set; } = null!;
    public string Direction { get; private set; } = null!;
    public string ConfidentialityLevel { get; private set; } = "NORMAL";
    public string PriorityLevel { get; private set; } = "NORMAL";
    public string CurrentStatusCode { get; private set; } = StatusDraft;
    public string? StatusBeforeArchiveCode { get; private set; }
    public Guid OwningDepartmentId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
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
        string? documentTypeCode,
        string? direction,
        Guid owningDepartmentId,
        Guid createdByUserId,
        string? subjectSummary = null,
        string confidentialityLevel = "NORMAL",
        string priorityLevel = "NORMAL",
        DateTime? documentDate = null,
        string? externalReferenceNumber = null,
        DateTime? externalDocumentDate = null)
    {
        var titleResult = DocumentTitle.Create(title);
        if (titleResult.IsFailure)
            return Result.Failure<Document>(titleResult.Error);

        if (string.IsNullOrWhiteSpace(documentTypeCode))
            return Result.Failure<Document>(DomainErrors.Correspondence.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(direction))
            return Result.Failure<Document>(DomainErrors.Correspondence.DirectionInvalid);

        var normalizedDirection = direction.Trim().ToUpperInvariant();
        if (normalizedDirection is not ("INTERNAL" or "INCOMING" or "OUTGOING"))
            return Result.Failure<Document>(DomainErrors.Correspondence.DirectionInvalid);

        var doc = new Document
        {
            DocumentId = Guid.NewGuid(),
            Title = titleResult.Value.Value,
            SubjectSummary = string.IsNullOrWhiteSpace(subjectSummary) ? null : subjectSummary.Trim(),
            DocumentTypeCode = documentTypeCode.Trim().ToUpperInvariant(),
            Direction = normalizedDirection,
            ConfidentialityLevel = string.IsNullOrWhiteSpace(confidentialityLevel) ? "NORMAL" : confidentialityLevel.Trim().ToUpperInvariant(),
            PriorityLevel = string.IsNullOrWhiteSpace(priorityLevel) ? "NORMAL" : priorityLevel.Trim().ToUpperInvariant(),
            CurrentStatusCode = StatusDraft,
            OwningDepartmentId = owningDepartmentId,
            CreatedByUserId = createdByUserId,
            DocumentDate = documentDate ?? DateTime.UtcNow,
            ExternalReferenceNumber = externalReferenceNumber,
            ExternalDocumentDate = externalDocumentDate,
            CreatedAt = DateTime.UtcNow
        };

        doc.AppendStatusHistory(null, StatusDraft, createdByUserId, "Initial draft created");
        doc.RaiseDomainEvent(new DocumentCreatedEvent(doc.DocumentId, doc.DocumentTypeCode, doc.Direction, doc.OwningDepartmentId, doc.CreatedByUserId));

        return Result.Success(doc);
    }

    public Result UpdateDraftMetadata(
        string? title,
        string? subjectSummary,
        string? confidentialityLevel = null,
        string? priorityLevel = null)
    {
        if (CurrentStatusCode != StatusDraft)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftEditable);

        var titleResult = DocumentTitle.Create(title);
        if (titleResult.IsFailure)
            return titleResult;

        Title = titleResult.Value.Value;
        SubjectSummary = string.IsNullOrWhiteSpace(subjectSummary) ? null : subjectSummary.Trim();

        if (!string.IsNullOrWhiteSpace(confidentialityLevel))
            ConfidentialityLevel = confidentialityLevel.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(priorityLevel))
            PriorityLevel = priorityLevel.Trim().ToUpperInvariant();

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result<DocumentParticipant> AddParticipant(
        string? roleCode,
        ParticipantTarget target,
        bool isPrimary = false,
        string? notes = null)
    {
        if (target.Kind == ParticipantTargetKind.External &&
            _participants.Any(p => p.ExternalOrganizationId.HasValue))
        {
            return Result.Failure<DocumentParticipant>(DomainErrors.Correspondence.SingleExternalPartyLimit);
        }

        var participantResult = DocumentParticipant.Create(DocumentId, roleCode, target, isPrimary, notes);
        if (participantResult.IsFailure)
            return participantResult;

        _participants.Add(participantResult.Value);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentParticipantAddedEvent(DocumentId, participantResult.Value.ParticipantId, participantResult.Value.RoleCode));
        return participantResult;
    }

    public Result Register(
        Guid officialNumberId,
        OfficialNumberValue officialNumberText,
        Guid actorUserId,
        string? notes = null)
    {
        if (CurrentStatusCode != StatusDraft)
            return Result.Failure(DomainErrors.Correspondence.OnlyDraftRegisterable);

        var previous = CurrentStatusCode;
        ActiveOfficialNumberId = officialNumberId;
        OfficialNumberText = officialNumberText.Value;
        RegistrationDate = DateTime.UtcNow;
        CurrentStatusCode = StatusRegistered;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, StatusRegistered, actorUserId, notes ?? $"Registered with number {officialNumberText.Value}");
        RaiseDomainEvent(new DocumentRegisteredEvent(DocumentId, officialNumberId, officialNumberText.Value, RegistrationDate.Value));
        return Result.Success();
    }

    public Result MarkInMovement(Guid actorUserId, string? notes = null)
    {
        if (CurrentStatusCode is StatusArchived or StatusCanceled)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusCode == StatusInMovement)
            return Result.Success();

        var previous = CurrentStatusCode;
        CurrentStatusCode = StatusInMovement;
        UpdatedAt = DateTime.UtcNow;
        AppendStatusHistory(previous, StatusInMovement, actorUserId, notes);
        return Result.Success();
    }

    public Result Archive(Guid actorUserId, bool hasActiveMovements, string? reason = null)
    {
        if (CurrentStatusCode == StatusArchived)
            return Result.Failure(DomainErrors.Correspondence.AlreadyArchived);

        if (CurrentStatusCode == StatusCanceled)
            return Result.Failure(DomainErrors.Correspondence.CannotArchiveCanceled);

        if (hasActiveMovements)
            return Result.Failure(DomainErrors.Correspondence.HasActiveMovements);

        var previous = CurrentStatusCode;
        StatusBeforeArchiveCode = previous;
        CurrentStatusCode = StatusArchived;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, StatusArchived, actorUserId, reason);
        RaiseDomainEvent(new DocumentArchivedEvent(DocumentId, previous, actorUserId, reason));
        return Result.Success();
    }

    public Result Unarchive(Guid actorUserId, string? reason = null)
    {
        if (CurrentStatusCode != StatusArchived)
            return Result.Failure(DomainErrors.Correspondence.NotArchived);

        var restored = string.IsNullOrWhiteSpace(StatusBeforeArchiveCode) ? StatusRegistered : StatusBeforeArchiveCode!;
        StatusBeforeArchiveCode = null;
        CurrentStatusCode = restored;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(StatusArchived, restored, actorUserId, reason);
        RaiseDomainEvent(new DocumentUnarchivedEvent(DocumentId, restored, actorUserId, reason));
        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, bool hasActiveMovements, string? reason)
    {
        if (CurrentStatusCode == StatusCanceled)
            return Result.Failure(DomainErrors.Correspondence.AlreadyCanceled);

        if (CurrentStatusCode == StatusArchived)
            return Result.Failure(DomainErrors.Correspondence.CannotCancelArchived);

        if (hasActiveMovements)
            return Result.Failure(DomainErrors.Correspondence.HasActiveMovements);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Correspondence.CancelReasonEmpty);

        var previous = CurrentStatusCode;
        CurrentStatusCode = StatusCanceled;
        UpdatedAt = DateTime.UtcNow;

        AppendStatusHistory(previous, StatusCanceled, actorUserId, reason.Trim());
        RaiseDomainEvent(new DocumentCanceledEvent(DocumentId, previous, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private void AppendStatusHistory(string? fromStatus, string toStatus, Guid changedByUserId, string? reason)
    {
        _statusHistory.Add(DocumentStatusHistory.Create(DocumentId, fromStatus, toStatus, changedByUserId, reason));
    }
}
