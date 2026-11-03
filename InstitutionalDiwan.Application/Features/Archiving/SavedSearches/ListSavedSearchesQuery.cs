using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>The caller's own saved searches, ordered by name.</summary>
public sealed record ListSavedSearchesQuery(Guid UserId) : IQuery<IReadOnlyCollection<SavedSearchResponse>>;
