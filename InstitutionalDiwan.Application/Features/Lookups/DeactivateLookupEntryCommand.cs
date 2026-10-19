using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>
/// Soft-deletes a reference-lookup entry so it can no longer be selected. Rejected with a conflict
/// when another table still references the entry.
/// </summary>
public sealed record DeactivateLookupEntryCommand(
    string Lookup,
    int Id) : ICommand<LookupEntryResponse>;
