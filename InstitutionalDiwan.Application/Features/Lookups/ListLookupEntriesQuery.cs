using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Lists the entries of one reference catalog addressed by its route slug.</summary>
public sealed record ListLookupEntriesQuery(
    string Lookup,
    bool IncludeInactive = false) : IQuery<IReadOnlyCollection<LookupEntryResponse>>;
