using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// Links the document <paramref name="DocumentId"/> to <paramref name="TargetDocumentId"/> with a
/// relation type. The caller needs DOWNLOAD access on the first document and VIEW on the second; the
/// first document must not be archived, and neither may be cancelled.
/// </summary>
public sealed record CreateDocumentRelationCommand(
    Guid DocumentId,
    Guid UserId,
    Guid TargetDocumentId,
    int RelationTypeId,
    string? Notes = null) : ICommand<DocumentRelationResponse>;
