using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Adds a new entry to a reference catalog with an administrator-chosen stable id.</summary>
public sealed record CreateLookupEntryCommand(
    string Lookup,
    int Id,
    string Desc,
    string DescAr) : ICommand<LookupEntryResponse>;
