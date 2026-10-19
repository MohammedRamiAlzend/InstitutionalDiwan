using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>Fetches a document with its participants and status history.</summary>
public sealed record GetDocumentDetailsQuery(Guid DocumentId)
    : IQuery<DocumentDetailResponse>;
