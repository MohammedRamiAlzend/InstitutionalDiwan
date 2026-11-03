using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>
/// Replaces the name, criteria, and default flag of one of the caller's saved searches. Another
/// user's search answers as not found, so its existence is not revealed.
/// </summary>
public sealed record UpdateSavedSearchCommand(
    Guid SavedSearchId,
    Guid UserId,
    string Name,
    string CriteriaJson,
    bool IsDefault) : ICommand<SavedSearchResponse>;
