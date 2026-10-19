using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Full document view: the flat document summary plus its participants and status history. Used by
/// the detail endpoint; list endpoints stay on the lighter <see cref="DocumentResponse"/>.
/// </summary>
public sealed record DocumentDetailResponse(
    DocumentResponse Document,
    IReadOnlyCollection<DocumentParticipantResponse> Participants,
    IReadOnlyCollection<DocumentStatusHistoryResponse> StatusHistory)
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
