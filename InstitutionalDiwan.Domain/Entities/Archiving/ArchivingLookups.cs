using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Archiving;

/// <summary>
/// Reference table of archive event types. Replaces the former string
/// <c>DocumentArchiveEvent.EventType</c>. Seeded by <c>ArchiveEventTypeSeeder</c>.
/// </summary>
public sealed class ArchiveEventType : LookupEntity
{
    public const int ArchivedId = 1;
    public const int UnarchivedId = 2;
    public const int RetentionAppliedId = 3;

    private ArchiveEventType()
    {
    }

    private ArchiveEventType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<ArchiveEventType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<ArchiveEventType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<ArchiveEventType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new ArchiveEventType(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of physical/electronic archive locations. Replaces the former string
/// <c>DocumentArchiveEvent.ArchiveLocationCode</c>. Seeded by <c>ArchiveLocationSeeder</c>.
/// </summary>
public sealed class ArchiveLocation : LookupEntity
{
    public const int CentralRegistryId = 1;
    public const int PhysicalArchiveId = 2;
    public const int ElectronicArchiveId = 3;

    private ArchiveLocation()
    {
    }

    private ArchiveLocation(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<ArchiveLocation> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<ArchiveLocation>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<ArchiveLocation>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new ArchiveLocation(id, desc, descAr));
    }
}
