using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Re-enables a previously deactivated reference-lookup entry.</summary>
public sealed record ActivateLookupEntryCommand(
    string Lookup,
    int Id) : ICommand<LookupEntryResponse>;
