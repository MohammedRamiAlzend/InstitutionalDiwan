using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// A request to delete a Drive item (Module 11). Deletion is approval-gated: the owner's
/// department head reviews the request; approval moves the item to the trash. Department
/// heads (and admins) may delete directly where their access allows, without a request.
/// </summary>
public sealed class DriveDeletionRequest : AggregateRoot<Guid>
{
    public const int MaxReasonLength = 500;
    public const int MaxReviewNoteLength = 500;

    public Guid RequestId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DriveItem.ItemId
    public Guid ItemId { get; private set; }
    // FK → User.UserId
    public Guid RequestedByUserId { get; private set; }
    public string Reason { get; private set; } = null!;
    public int StatusId { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsPending => StatusId == DriveDeletionRequestStatus.PendingId;

    private DriveDeletionRequest()
    {
    }

    public static Result<DriveDeletionRequest> Create(
        Guid itemId,
        Guid requestedByUserId,
        string? reason,
        DateTime? utcNow = null)
    {
        if (itemId == Guid.Empty)
            return Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.ItemNotFound);
        if (requestedByUserId == Guid.Empty)
            return Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.PrincipalIdEmpty);

        var trimmed = reason?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.RequestReasonEmpty);
        if (trimmed.Length > MaxReasonLength)
            return Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.RequestReasonTooLong);

        var request = new DriveDeletionRequest
        {
            RequestId = Guid.NewGuid(),
            ItemId = itemId,
            RequestedByUserId = requestedByUserId,
            Reason = trimmed,
            StatusId = DriveDeletionRequestStatus.PendingId,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };
        request.RaiseDomainEvent(new DriveDeletionRequestedEvent(request.RequestId, itemId, requestedByUserId));
        return Result.Success(request);
    }

    public Result Approve(Guid reviewerUserId, string? note = null, DateTime? utcNow = null) =>
        Review(reviewerUserId, DriveDeletionRequestStatus.ApprovedId, note, utcNow);

    public Result Reject(Guid reviewerUserId, string? note = null, DateTime? utcNow = null) =>
        Review(reviewerUserId, DriveDeletionRequestStatus.RejectedId, note, utcNow);

    private Result Review(Guid reviewerUserId, int statusId, string? note, DateTime? utcNow)
    {
        if (!IsPending)
            return Result.Failure(DomainErrors.Drive.RequestAlreadyReviewed);
        if (reviewerUserId == Guid.Empty)
            return Result.Failure(DomainErrors.Drive.PrincipalIdEmpty);

        var trimmedNote = note?.Trim();
        if (trimmedNote is { Length: > MaxReviewNoteLength })
            return Result.Failure(DomainErrors.Drive.ReviewNoteTooLong);

        StatusId = statusId;
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = utcNow ?? DateTime.UtcNow;
        ReviewNote = string.IsNullOrWhiteSpace(trimmedNote) ? null : trimmedNote;

        RaiseDomainEvent(statusId == DriveDeletionRequestStatus.ApprovedId
            ? new DriveDeletionApprovedEvent(RequestId, ItemId, reviewerUserId)
            : new DriveDeletionRejectedEvent(RequestId, ItemId, reviewerUserId));
        return Result.Success();
    }

    public Result Cancel(Guid userId)
    {
        if (!IsPending)
            return Result.Failure(DomainErrors.Drive.RequestAlreadyReviewed);
        if (userId != RequestedByUserId)
            return Result.Failure(DomainErrors.Drive.OnlyRequesterCanCancel);

        StatusId = DriveDeletionRequestStatus.CancelledId;
        return Result.Success();
    }
}