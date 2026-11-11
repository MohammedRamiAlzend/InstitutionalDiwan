using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>
/// Where a document stands against the designated archive diwan: whether one is designated, and
/// whether the document's latest movement that was not canceled ended there and was received.
/// </summary>
internal sealed record ArchiveDiwanCustody(Guid? ArchiveDiwanDepartmentId, DocumentMovement? LatestMovement)
{
    /// <summary>True when an archive diwan is designated and holds the document.</summary>
    public bool IsHeld =>
        ArchiveDiwanDepartmentId is { } archiveDiwanDepartmentId &&
        LatestMovement?.IsHeldAt(archiveDiwanDepartmentId) == true;

    public static async Task<ArchiveDiwanCustody> ResolveAsync(
        Guid documentId,
        IInstitutionSettingRepository settingRepository,
        IDocumentMovementRepository movementRepository,
        CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetSingletonAsync(cancellationToken);
        if (setting?.ArchiveDiwanDepartmentId is not { } archiveDiwanDepartmentId ||
            archiveDiwanDepartmentId == Guid.Empty)
        {
            return new ArchiveDiwanCustody(null, null);
        }

        var movements = await movementRepository.ListByDocumentAsync(documentId, cancellationToken);
        var latest = movements.FirstOrDefault(movement => movement.StatusId != DocumentMovement.StatusCanceled);
        return new ArchiveDiwanCustody(archiveDiwanDepartmentId, latest);
    }
}
