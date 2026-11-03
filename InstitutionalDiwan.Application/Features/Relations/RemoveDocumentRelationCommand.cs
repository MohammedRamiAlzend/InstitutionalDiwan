using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// Removes a link of the document <paramref name="DocumentId"/>. The reason is required because a link
/// is part of the record; the removal stays in the relation history after the row is gone.
/// </summary>
public sealed record RemoveDocumentRelationCommand(
    Guid DocumentId,
    Guid RelationId,
    Guid UserId,
    string Reason) : ICommand;
