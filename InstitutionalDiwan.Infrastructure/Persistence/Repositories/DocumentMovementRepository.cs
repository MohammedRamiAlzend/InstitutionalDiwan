using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentMovementRepository : IDocumentMovementRepository
{
    private readonly DiwanDbContext _context;

    public DocumentMovementRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentMovement?> GetByIdAsync(
        Guid movementId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentMovements
            .Include(movement => movement.Steps)
            .Include(movement => movement.Events)
            .FirstOrDefaultAsync(movement => movement.MovementId == movementId, cancellationToken);

    /// <summary>
    /// A movement is active until it reaches a terminal status (CLOSED or CANCELED); every state in
    /// between still holds the document.
    /// </summary>
    public Task<bool> HasActiveMovementsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentMovements.AnyAsync(
            movement => movement.DocumentId == documentId &&
                        movement.StatusId != DocumentMovement.StatusClosed &&
                        movement.StatusId != DocumentMovement.StatusCanceled,
            cancellationToken);

    public void Add(DocumentMovement movement) => _context.DocumentMovements.Add(movement);
}
