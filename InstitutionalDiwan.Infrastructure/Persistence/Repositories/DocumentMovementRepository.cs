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

    /// <summary>Every movement of a document, newest first, with its steps and events.</summary>
    public async Task<IReadOnlyList<DocumentMovement>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentMovements
            .Include(movement => movement.Steps)
            .Include(movement => movement.Events)
            .Where(movement => movement.DocumentId == documentId)
            .OrderByDescending(movement => movement.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Movements waiting in a department's inbox, newest first. The current step is addressed to
    /// that department and has not left it yet (sent, received, or completed), so the document
    /// still awaits the department's action; plus the documents that came back to it because the
    /// receiving department rejected or returned them. Closed and canceled movements are left out.
    /// </summary>
    public async Task<IReadOnlyList<DocumentMovement>> ListInboxAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentMovements
            .Include(movement => movement.Steps)
            .Include(movement => movement.Events)
            .Where(movement => movement.StatusId != DocumentMovement.StatusClosed &&
                               movement.StatusId != DocumentMovement.StatusCanceled &&
                               movement.Steps.Any(step =>
                                   step.StepNo == movement.CurrentStepNo &&
                                   ((step.ToDepartmentId == departmentId &&
                                     (step.StepStatusId == MovementStepStatus.SentId ||
                                      step.StepStatusId == MovementStepStatus.ReceivedId ||
                                      step.StepStatusId == MovementStepStatus.CompletedId)) ||
                                    (step.FromDepartmentId == departmentId &&
                                     (step.StepStatusId == MovementStepStatus.RejectedId ||
                                      step.StepStatusId == MovementStepStatus.ReturnedId)))))
            .OrderByDescending(movement => movement.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Movements a department sent out and that are still with the receiving side, newest first.
    /// The origin of the current step is the department that handed the document over, which is
    /// not the movement's source department once the document has been redirected onwards.
    /// </summary>
    public async Task<IReadOnlyList<DocumentMovement>> ListOutboxAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentMovements
            .Include(movement => movement.Steps)
            .Include(movement => movement.Events)
            .Where(movement => movement.StatusId != DocumentMovement.StatusClosed &&
                               movement.StatusId != DocumentMovement.StatusCanceled &&
                               movement.Steps.Any(step =>
                                   step.StepNo == movement.CurrentStepNo &&
                                   step.FromDepartmentId == departmentId &&
                                   (step.StepStatusId == MovementStepStatus.SentId ||
                                    step.StepStatusId == MovementStepStatus.ReceivedId ||
                                    step.StepStatusId == MovementStepStatus.CompletedId)))
            .OrderByDescending(movement => movement.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <summary>The non-terminal movement that currently holds the document, if there is one.</summary>
    public Task<DocumentMovement?> GetActiveByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentMovements
            .Include(movement => movement.Steps)
            .Include(movement => movement.Events)
            .Where(movement => movement.DocumentId == documentId &&
                               movement.StatusId != DocumentMovement.StatusClosed &&
                               movement.StatusId != DocumentMovement.StatusCanceled)
            .OrderByDescending(movement => movement.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

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
