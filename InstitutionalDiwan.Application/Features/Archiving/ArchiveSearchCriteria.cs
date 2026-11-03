using System.Text.Json;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>
/// The stored shape of a saved archive search. The server only checks it: the client reads the
/// criteria back and runs them through the archive search, where access is enforced.
/// </summary>
public sealed record ArchiveSearchCriteria(
    string? Search = null,
    string? SortBy = null,
    bool? SortDescending = null,
    Guid? OwningDepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null,
    int? ArchiveLocationId = null,
    DateTime? ArchivedFromUtc = null,
    DateTime? ArchivedToUtc = null)
{
    private static readonly string[] SortKeys = { "archivedAt", "title", "officialNumber" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>True when <paramref name="criteriaJson"/> is a JSON object using only the known fields.</summary>
    public static bool IsValid(string? criteriaJson)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson))
            return false;

        try
        {
            var criteria = JsonSerializer.Deserialize<ArchiveSearchCriteria>(criteriaJson, JsonOptions);

            return criteria is not null &&
                   (criteria.SortBy is null ||
                    SortKeys.Contains(criteria.SortBy, StringComparer.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
