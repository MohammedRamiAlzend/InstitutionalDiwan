using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Fetches a single reference-lookup entry (active or inactive) by id.</summary>
public sealed record GetLookupEntryByIdQuery(
    string Lookup,
    int Id) : IQuery<LookupEntryResponse>;
