using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Transitions a DRAFT document to REGISTERED. The official number is allocated gap-free from the
/// registry matching the document's department/document type/direction scope and rendered through
/// the registry's <c>FormatPattern</c>. Defaults to the current year when
/// <paramref name="NumberingYear"/> is omitted.
/// </summary>
public sealed record RegisterDocumentCommand(
    Guid DocumentId,
    Guid UserId,
    int? NumberingYear = null) : ICommand<DocumentResponse>;
