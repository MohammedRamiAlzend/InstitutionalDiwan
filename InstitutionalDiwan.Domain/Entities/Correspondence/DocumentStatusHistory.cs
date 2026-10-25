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
    // FK → DocumentStatusType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public int? FromStatusId { get; private set; }
    // FK → DocumentStatusType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public int ToStatusId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ChangedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private DocumentStatusHistory()
    {
    }

    internal static DocumentStatusHistory Create(
        Guid documentId,
        int? fromStatusId,
        int toStatusId,
        Guid changedByUserId,
        string? reason = null,
        DateTime? utcNow = null)
    {
        return new DocumentStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = documentId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedByUserId = changedByUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ChangedAt = utcNow ?? DateTime.UtcNow
        };
    }
}
