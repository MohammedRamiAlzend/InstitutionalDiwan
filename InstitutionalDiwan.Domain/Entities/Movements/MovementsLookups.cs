using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Movements;

/// <summary>
/// Reference table classifying a document movement. Replaces the former string
/// <c>DocumentMovement.MovementType</c>. Seeded by <c>MovementTypeSeeder</c>.
/// </summary>
public sealed class MovementType : LookupEntity
{
    public const int ReferralId = 1;
    public const int TransferId = 2;
    public const int CcId = 3;
    public const int ReturnId = 4;

    private MovementType()
    {
    }

    private MovementType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<MovementType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<MovementType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<MovementType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new MovementType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of a movement's lifecycle status. Replaces the former string
/// <c>DocumentMovement.StatusCode</c>. The ids are aliased by <see cref="DocumentMovement"/>'s
/// <c>Status*</c> constants. Seeded by <c>MovementStatusSeeder</c>.
/// </summary>
public sealed class MovementStatus : LookupEntity
{
    public const int DraftId = 1;
    public const int SentId = 2;
    public const int ReceivedId = 3;
    public const int InProgressId = 4;
    public const int CompletedId = 5;
    public const int RejectedId = 6;
    public const int ReturnedId = 7;
    public const int RedirectedId = 8;
    public const int ClosedId = 9;
    public const int CanceledId = 10;

    private MovementStatus()
    {
    }

    private MovementStatus(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<MovementStatus> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<MovementStatus>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<MovementStatus>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new MovementStatus(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of a movement step's status. Replaces the former string
/// <c>DocumentMovementStep.StepStatus</c>. Seeded by <c>MovementStepStatusSeeder</c>.
/// </summary>
public sealed class MovementStepStatus : LookupEntity
{
    public const int PendingId = 1;
    public const int SentId = 2;
    public const int ReceivedId = 3;
    public const int CompletedId = 4;
    public const int RejectedId = 5;
    public const int ReturnedId = 6;
    public const int RedirectedId = 7;

    private MovementStepStatus()
    {
    }

    private MovementStepStatus(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<MovementStepStatus> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<MovementStepStatus>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<MovementStepStatus>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new MovementStepStatus(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of the events recorded against a movement. Replaces the former string
/// <c>DocumentMovementEvent.EventType</c>. Seeded by <c>MovementEventTypeSeeder</c>.
/// </summary>
public sealed class MovementEventType : LookupEntity
{
    public const int CreatedId = 1;
    public const int SentId = 2;
    public const int ReceivedId = 3;
    public const int CompletedId = 4;
    public const int RejectedId = 5;
    public const int ReturnedId = 6;
    public const int RedirectedId = 7;
    public const int ClosedId = 8;
    public const int CanceledId = 9;

    private MovementEventType()
    {
    }

    private MovementEventType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<MovementEventType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<MovementEventType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<MovementEventType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new MovementEventType(id, desc, descAr));
    }
}
