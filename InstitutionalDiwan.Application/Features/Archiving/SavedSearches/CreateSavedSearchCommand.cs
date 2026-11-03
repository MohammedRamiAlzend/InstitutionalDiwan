using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>
/// Saves an archive search for the caller. When <paramref name="IsDefault"/> is true it replaces the
/// caller's current default.
/// </summary>
public sealed record CreateSavedSearchCommand(
    Guid UserId,
    string Name,
    string CriteriaJson,
    bool IsDefault = false) : ICommand<SavedSearchResponse>;
