using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class DocumentStatusHistory : Entity<Guid>
{
    public Guid HistoryId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → DocumentStatusType.Code (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public string? FromStatusCode { get; private set; }
    // FK → DocumentStatusType.Code (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public string ToStatusCode { get; private set; } = null!;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ChangedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private DocumentStatusHistory()
    {
    }

    internal static DocumentStatusHistory Create(
        Guid documentId,
        string? fromStatusCode,
        string toStatusCode,
        Guid changedByUserId,
        string? reason = null)
    {
        return new DocumentStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = documentId,
            FromStatusCode = fromStatusCode,
            ToStatusCode = toStatusCode,
            ChangedByUserId = changedByUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ChangedAt = DateTime.UtcNow
        };
    }
}
