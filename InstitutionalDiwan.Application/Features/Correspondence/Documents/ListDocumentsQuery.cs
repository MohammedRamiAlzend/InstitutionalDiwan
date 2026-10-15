using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>Lists the most recent documents (newest first).</summary>
public sealed record ListDocumentsQuery : IQuery<IReadOnlyCollection<DocumentResponse>>;
