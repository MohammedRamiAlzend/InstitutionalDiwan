using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Lookups;

/// <summary>Updates the English and Arabic labels of an existing reference-lookup entry.</summary>
public sealed record RelabelLookupEntryCommand(
    string Lookup,
    int Id,
    string Desc,
    string DescAr) : ICommand<LookupEntryResponse>;
