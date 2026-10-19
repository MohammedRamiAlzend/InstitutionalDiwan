using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class GetDocumentTimelineQueryHandler
    : IQueryHandler<GetDocumentTimelineQuery, IReadOnlyCollection<DocumentTimelineEntryResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;

    public GetDocumentTimelineQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
    }

    public async Task<Result<IReadOnlyCollection<DocumentTimelineEntryResponse>>> Handle(
        GetDocumentTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<IReadOnlyCollection<DocumentTimelineEntryResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);

        var entries = new List<DocumentTimelineEntryResponse>();

        entries.AddRange(document.StatusHistory.Select(history => new DocumentTimelineEntryResponse(
            EventKind: "STATUS",
            OccurredAt: history.ChangedAt,
            ActorUserId: history.ChangedByUserId,
            Description: history.Reason,
            FromStatusId: history.FromStatusId,
            ToStatusId: history.ToStatusId)));

        entries.AddRange(document.Participants.Select(participant => new DocumentTimelineEntryResponse(
            EventKind: "PARTICIPANT",
            OccurredAt: participant.CreatedAt,
            ActorUserId: document.CreatedByUserId,
            Description: participant.PersonName,
            ParticipantId: participant.ParticipantId)));

        var numbers = await _documentNumberRepository.ListByDocumentAsync(
            document.DocumentId, cancellationToken);

        foreach (var number in numbers)
        {
            entries.AddRange(number.History.Select(history => new DocumentTimelineEntryResponse(
                EventKind: "NUMBER",
                OccurredAt: history.OccurredAt,
                ActorUserId: history.ActorUserId,
                Description: DescribeNumberEvent(number, history.EventTypeId, history.Notes),
                DocumentNumberId: number.DocumentNumberId,
                NumberEventTypeId: history.EventTypeId)));
        }

        IReadOnlyCollection<DocumentTimelineEntryResponse> timeline = entries
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.EventKind, StringComparer.Ordinal)
            .ToList();

        return Result.Success(timeline);
    }

    private static string DescribeNumberEvent(DocumentNumber number, int eventTypeId, string? notes)
    {
        var action = eventTypeId switch
        {
            DocumentNumberEventType.IssuedId => "issued",
            DocumentNumberEventType.CancelledId => "cancelled",
            DocumentNumberEventType.ReplacedId => "replaced",
            _ => "updated"
        };

        return string.IsNullOrWhiteSpace(notes)
            ? $"Official number {number.OfficialNumberText} {action}"
            : $"Official number {number.OfficialNumberText} {action}: {notes}";
    }
}
