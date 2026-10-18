using System;
using System.Collections.Generic;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Events;

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

    public IReadOnlyCollection<DocumentNumberHistory> History => _history.AsReadOnly();

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
            NumberingYear = numberingYear,
            SequenceValue = sequenceValue,
            OfficialNumberText = numberValResult.Value.Value,
            StatusId = DocumentNumberStatus.ActiveId,
            IssuedByUserId = issuedByUserId,
            IssuedAt = DateTime.UtcNow
        };

        docNumber._history.Add(DocumentNumberHistory.Create(docNumber.DocumentNumberId, DocumentNumberEventType.IssuedId, issuedByUserId, notes));
        docNumber.RaiseDomainEvent(new OfficialNumberIssuedEvent(
            docNumber.DocumentNumberId,
            docNumber.DocumentId,
            docNumber.RegistryId,
            docNumber.OfficialNumberText,
            docNumber.NumberingYear,
            docNumber.SequenceValue));

        return Result.Success(docNumber);
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
}
