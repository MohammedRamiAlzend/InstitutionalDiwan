using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>Fetches a single document by id.</summary>
public sealed record GetDocumentByIdQuery(Guid DocumentId) : IQuery<DocumentResponse>;
