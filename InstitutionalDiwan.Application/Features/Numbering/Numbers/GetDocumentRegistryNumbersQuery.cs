using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>Returns the central number and every department register line for a document.</summary>
public sealed record GetDocumentRegistryNumbersQuery(Guid DocumentId)
    : IQuery<DocumentRegistryNumbersResponse>;
