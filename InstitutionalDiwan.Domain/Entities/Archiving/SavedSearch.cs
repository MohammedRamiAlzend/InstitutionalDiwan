using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Archiving;

public sealed class SavedSearch : AggregateRoot<Guid>
{
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
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<SavedSearch>(DomainErrors.Archiving.SearchNameEmpty);

        if (string.IsNullOrWhiteSpace(criteriaJson))
            return Result.Failure<SavedSearch>(DomainErrors.Archiving.CriteriaJsonEmpty);

        var savedSearch = new SavedSearch
        {
            SavedSearchId = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CriteriaJson = criteriaJson.Trim(),
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(savedSearch);
    }

    public Result UpdateCriteria(string? name, string? criteriaJson, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DomainErrors.Archiving.SearchNameEmpty);

        if (string.IsNullOrWhiteSpace(criteriaJson))
            return Result.Failure(DomainErrors.Archiving.CriteriaJsonEmpty);

        Name = name.Trim();
        CriteriaJson = criteriaJson.Trim();
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
