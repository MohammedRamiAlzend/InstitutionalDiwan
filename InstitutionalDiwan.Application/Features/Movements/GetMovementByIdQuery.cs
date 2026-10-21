using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>Fetches a single movement with its steps and audit trail.</summary>
public sealed record GetMovementByIdQuery(Guid MovementId) : IQuery<MovementResponse>;
