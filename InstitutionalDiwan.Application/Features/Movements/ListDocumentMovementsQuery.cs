using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>Lists every movement of a document, newest first.</summary>
public sealed record ListDocumentMovementsQuery(Guid DocumentId)
    : IQuery<IReadOnlyCollection<MovementResponse>>;
