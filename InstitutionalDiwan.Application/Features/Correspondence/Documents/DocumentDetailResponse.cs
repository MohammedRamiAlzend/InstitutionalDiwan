using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Full document view: the flat document summary plus its participants and status history. Used by
/// the detail endpoint; list endpoints stay on the lighter <see cref="DocumentResponse"/>.
/// <see cref="ArchiveDiwanDepartmentId"/> is the designated archive diwan (null when archiving stays
/// with the owning department) and <see cref="IsHeldByArchiveDiwan"/> tells whether that department
/// holds the document now, so a client can offer archive and unarchive to the right department.
/// </summary>
public sealed record DocumentDetailResponse(
    DocumentResponse Document,
    IReadOnlyCollection<DocumentParticipantResponse> Participants,
    IReadOnlyCollection<DocumentStatusHistoryResponse> StatusHistory,
    Guid? ArchiveDiwanDepartmentId = null,
    bool IsHeldByArchiveDiwan = false)
{
    public static DocumentDetailResponse From(Document document) => new(
        DocumentResponse.From(document),
        document.Participants
            .OrderBy(participant => participant.CreatedAt)
            .Select(DocumentParticipantResponse.From)
            .ToList(),
        document.StatusHistory
            .OrderBy(history => history.ChangedAt)
            .Select(DocumentStatusHistoryResponse.From)
            .ToList());
}
