using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>
/// Makes an active reference-lookup entry its catalog's only default (the entry selects preselect),
/// or, with <c>IsDefault = false</c>, removes the default mark. Setting a new default clears the
/// previous one in the same unit of work.
/// </summary>
public sealed record SetLookupEntryDefaultCommand(
    string Lookup,
    int Id,
    bool IsDefault) : ICommand<LookupEntryResponse>;
