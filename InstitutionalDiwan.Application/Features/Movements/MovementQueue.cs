using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Shared plumbing for the department inbox and outbox queries: resolving which department the
/// caller is asking about, and turning movements into document-aware queue rows.
/// </summary>
internal static class MovementQueue
{
    /// <summary>
    /// Resolves the department a queue query targets. Without an explicit department it is the
    /// caller's own primary department; an explicit one must sit inside the caller's
    /// organizational scope, so a supervisor can read a sub-department's queue but nobody can
    /// peek at a department they have no business seeing.
    /// </summary>
    public static async Task<Result<Guid>> ResolveDepartmentIdAsync(
        Guid userId,
        Guid? requestedDepartmentId,
        IUserRepository userRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<Guid>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<Guid>(DomainErrors.Identity.UserInactive);

        if (requestedDepartmentId is not { } departmentId)
            return Result.Success(user.PrimaryDepartmentId);

        if (departmentId == user.PrimaryDepartmentId)
            return Result.Success(departmentId);

        var canAccess = await departmentScopeEvaluator.CanAccessDepartmentAsync(
            departmentId, cancellationToken);

        return canAccess
            ? Result.Success(departmentId)
            : Result.Failure<Guid>(DomainErrors.Movements.DepartmentOutOfScope);
    }

    /// <summary>
    /// Turns movements into queue rows, resolving the document title and official number with a
    /// single batch lookup.
    /// </summary>
    public static async Task<IReadOnlyCollection<MovementQueueItemResponse>> BuildAsync(
        IReadOnlyList<DocumentMovement> movements,
        IDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        if (movements.Count == 0)
            return Array.Empty<MovementQueueItemResponse>();

        var documentIds = movements
            .Select(movement => movement.DocumentId)
            .Distinct()
            .ToList();

        var documents = await documentRepository.GetByIdsAsync(documentIds, cancellationToken);
        var documentsById = documents.ToDictionary(document => document.DocumentId);

        return movements
            .Select(movement =>
            {
                documentsById.TryGetValue(movement.DocumentId, out var document);

                return MovementQueueItemResponse.From(
                    movement,
                    document?.Title ?? string.Empty,
                    document?.OfficialNumberText);
            })
            .ToList();
    }
}
