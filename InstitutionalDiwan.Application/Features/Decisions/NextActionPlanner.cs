using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Decisions;

/// <summary>
/// The single suggested step after a decision, or when the archive diwan is holding the file.
/// A referral points at the type's destination. A send-to-archive decision follows the archive
/// route from wherever the file is now. Nothing is suggested while the file is in transit.
/// </summary>
internal static class NextActionPlanner
{
    public static NextActionResponse Plan(
        Document document,
        DocumentDecision? active,
        OpenDecisionVisit? visit,
        Guid? archiveDiwanDepartmentId,
        bool heldByArchiveDiwan)
    {
        if (document.CurrentStatusId is DocumentStatusType.ArchivedId or DocumentStatusType.CanceledId)
            return None();

        var holder = visit?.DepartmentId;
        if (holder is null && heldByArchiveDiwan)
            holder = archiveDiwanDepartmentId;

        if (active is { IsInForce: true, EffectId: DecisionEffect.ReferId, DestinationDepartmentId: { } destination }
            && holder is { } holding
            && holding != destination)
        {
            return new NextActionResponse(NextActionKind.Refer, active.DecisionId, destination, MovementType.ReferralId);
        }

        var sendingToArchive = active is { IsInForce: true, EffectId: DecisionEffect.SendToArchiveId };
        if (!sendingToArchive && !heldByArchiveDiwan)
            return None();

        if (heldByArchiveDiwan || (archiveDiwanDepartmentId is { } archiveId && holder == archiveId))
            return new NextActionResponse(NextActionKind.Archive, active?.DecisionId, null, null);

        if (holder is not { } departmentId)
            return None();

        if (archiveDiwanDepartmentId is null)
        {
            return departmentId == document.OwningDepartmentId
                ? new NextActionResponse(NextActionKind.Archive, active?.DecisionId, null, null)
                : None();
        }

        var next = departmentId == document.OwningDepartmentId
            ? archiveDiwanDepartmentId.Value
            : document.OwningDepartmentId;

        return new NextActionResponse(
            NextActionKind.SendForArchive, active?.DecisionId, next, MovementType.ToArchiveId);
    }

    private static NextActionResponse None() => new(null, null, null, null);
}
