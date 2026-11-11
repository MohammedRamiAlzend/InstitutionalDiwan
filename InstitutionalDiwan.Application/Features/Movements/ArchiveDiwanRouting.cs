using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// The route a document takes to the archive diwan. Any department that wants a document archived
/// sends it, in a movement of type <see cref="MovementType.ToArchiveId"/>, to the department that
/// created it; only that department sends it on to the archive diwan, and the archive diwan receives
/// nothing else. The Central Diwan may relay such a movement when the creating department is not the
/// Central Diwan itself, because every hop still has to pass the hub. While no archive diwan is
/// designated the rules are off and archiving stays with the creating department.
/// </summary>
internal sealed class ArchiveDiwanRouting
{
    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IDocumentRepository _documentRepository;

    public ArchiveDiwanRouting(
        IInstitutionSettingRepository settingRepository,
        IDocumentRepository documentRepository)
    {
        _settingRepository = settingRepository;
        _documentRepository = documentRepository;
    }

    /// <summary>The designated archive diwan, or null when none is designated.</summary>
    public async Task<Guid?> GetArchiveDiwanDepartmentIdAsync(CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        return setting?.ArchiveDiwanDepartmentId is { } id && id != Guid.Empty ? id : null;
    }

    /// <summary>Validates the first hop of a movement that is being opened or sent.</summary>
    public Task<Result> ValidateFirstHopAsync(
        Guid documentId,
        int movementTypeId,
        Guid sourceDepartmentId,
        Guid destinationDepartmentId,
        CancellationToken cancellationToken) =>
        ValidateAsync(
            documentId, movementTypeId, sourceDepartmentId, destinationDepartmentId,
            visitedDepartmentIds: [sourceDepartmentId], cancellationToken);

    /// <summary>
    /// Validates the next hop of a movement that is being redirected, under the type the movement
    /// carries from this hop on.
    /// </summary>
    public Task<Result> ValidateNextHopAsync(
        DocumentMovement movement,
        Guid newDestinationDepartmentId,
        int movementTypeId,
        CancellationToken cancellationToken) =>
        ValidateAsync(
            movement.DocumentId,
            movementTypeId,
            movement.DestinationDepartmentId,
            newDestinationDepartmentId,
            visitedDepartmentIds: movement.Steps
                .SelectMany(step => new[] { step.FromDepartmentId, step.ToDepartmentId })
                .ToHashSet(),
            cancellationToken);

    private async Task<Result> ValidateAsync(
        Guid documentId,
        int movementTypeId,
        Guid sourceDepartmentId,
        Guid destinationDepartmentId,
        IReadOnlyCollection<Guid> visitedDepartmentIds,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        var archiveDiwanDepartmentId =
            setting?.ArchiveDiwanDepartmentId is { } id && id != Guid.Empty ? id : (Guid?)null;
        var isArchiveMovement = movementTypeId == MovementType.ToArchiveId;

        if (archiveDiwanDepartmentId is not { } archiveDiwanId)
        {
            return isArchiveMovement
                ? Result.Failure(DomainErrors.Archiving.ArchiveDiwanNotConfigured)
                : Result.Success();
        }

        if (!isArchiveMovement)
        {
            // Work handed over inside the archive diwan itself is not an arrival.
            return destinationDepartmentId == archiveDiwanId && sourceDepartmentId != archiveDiwanId
                ? Result.Failure(DomainErrors.Archiving.ArchiveDiwanAcceptsArchiveMovementsOnly)
                : Result.Success();
        }

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return Result.Failure(DomainErrors.Correspondence.DocumentNotFound);

        var owningDepartmentId = document.OwningDepartmentId;
        var centralDiwanDepartmentId = setting!.CentralDiwanDepartmentId;

        if (destinationDepartmentId == archiveDiwanId)
        {
            var sentByOwner = sourceDepartmentId == owningDepartmentId;
            var relayedByCentralDiwan =
                sourceDepartmentId == centralDiwanDepartmentId &&
                visitedDepartmentIds.Contains(owningDepartmentId);

            return sentByOwner || relayedByCentralDiwan
                ? Result.Success()
                : Result.Failure(DomainErrors.Archiving.OnlyOwningDepartmentSendsToArchive);
        }

        return destinationDepartmentId == owningDepartmentId ||
               destinationDepartmentId == centralDiwanDepartmentId
            ? Result.Success()
            : Result.Failure(DomainErrors.Archiving.ArchiveRouteViaOwningDepartment);
    }
}
