using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>
/// Archives a registered document at an active archive location. Rejected while the document has
/// active movements, and for drafts and canceled documents.
/// </summary>
public sealed record ArchiveDocumentCommand(
    Guid DocumentId,
    Guid UserId,
    int ArchiveLocationId,
    string? Reason = null) : ICommand<DocumentResponse>;
