namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>A user's personal favorite marker on a Drive item (Module 11); never inherited or shared.</summary>
public sealed class DriveStar : Entity<Guid>
{
    public Guid StarId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → User.UserId
    public Guid UserId { get; private set; }
    // FK → DriveItem.ItemId
    public Guid ItemId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DriveStar()
    {
    }

    public static Result<DriveStar> Create(Guid userId, Guid itemId, DateTime? utcNow = null)
    {
        if (userId == Guid.Empty || itemId == Guid.Empty)
            return Result.Failure<DriveStar>(DomainErrors.Drive.PrincipalIdEmpty);

        var star = new DriveStar
        {
            StarId = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };
        return Result.Success(star);
    }
}