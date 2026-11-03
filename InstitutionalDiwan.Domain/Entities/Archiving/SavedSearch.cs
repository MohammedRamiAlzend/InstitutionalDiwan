namespace InstitutionalDiwan.Domain.Entities.Archiving;

public sealed class SavedSearch : AggregateRoot<Guid>
{
    public const int MaxNameLength = 150;
    public const int MaxCriteriaLength = 4000;
    public const int MaxPerUser = 50;

    public Guid SavedSearchId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string CriteriaJson { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SavedSearch()
    {
    }

    public static Result<SavedSearch> Create(
        Guid userId,
        string? name,
        string? criteriaJson,
        bool isDefault = false,
        DateTime? utcNow = null)
    {
        var valid = Validate(name, criteriaJson);
        if (valid.IsFailure)
            return Result.Failure<SavedSearch>(valid.Error);

        var savedSearch = new SavedSearch
        {
            SavedSearchId = Guid.NewGuid(),
            UserId = userId,
            Name = name!.Trim(),
            CriteriaJson = criteriaJson!.Trim(),
            IsDefault = isDefault,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(savedSearch);
    }

    public Result UpdateCriteria(string? name, string? criteriaJson, bool isDefault, DateTime? utcNow = null)
    {
        var valid = Validate(name, criteriaJson);
        if (valid.IsFailure)
            return valid;

        Name = name!.Trim();
        CriteriaJson = criteriaJson!.Trim();
        IsDefault = isDefault;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Drops the default flag when another of the user's searches becomes their default.</summary>
    public void ClearDefault(DateTime? utcNow = null)
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    private static Result Validate(string? name, string? criteriaJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DomainErrors.Archiving.SearchNameEmpty);

        if (name.Trim().Length > MaxNameLength)
            return Result.Failure(DomainErrors.Archiving.SearchNameTooLong);

        if (string.IsNullOrWhiteSpace(criteriaJson))
            return Result.Failure(DomainErrors.Archiving.CriteriaJsonEmpty);

        if (criteriaJson.Trim().Length > MaxCriteriaLength)
            return Result.Failure(DomainErrors.Archiving.CriteriaInvalid);

        return Result.Success();
    }
}
