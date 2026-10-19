using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Cancels a document with a required reason. Cancellation is rejected while the document has
/// active movements and for archived documents (they must be unarchived first).
/// </summary>
public sealed record CancelDocumentCommand(
    Guid DocumentId,
    Guid UserId,
    string Reason) : ICommand<DocumentResponse>;
