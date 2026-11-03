using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

/// <summary>
/// Permanently deletes one of the caller's saved searches. It is a personal preference, not an
/// institutional record. Another user's search answers as not found.
/// </summary>
public sealed record DeleteSavedSearchCommand(Guid SavedSearchId, Guid UserId) : ICommand;
