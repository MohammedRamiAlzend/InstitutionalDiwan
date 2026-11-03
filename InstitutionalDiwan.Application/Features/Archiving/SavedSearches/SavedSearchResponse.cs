using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>Read model returned by the saved search endpoints.</summary>
public sealed record SavedSearchResponse(
    Guid SavedSearchId,
    string Name,
    string CriteriaJson,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static SavedSearchResponse From(SavedSearch savedSearch) => new(
        savedSearch.SavedSearchId,
        savedSearch.Name,
        savedSearch.CriteriaJson,
        savedSearch.IsDefault,
        savedSearch.CreatedAt,
        savedSearch.UpdatedAt);
}
