using InstitutionalDiwan.Domain.Entities.Numbering;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>An official number issued for a document, with its full event history.</summary>
public sealed record DocumentNumberResponse(
    Guid DocumentNumberId,
    Guid DocumentId,
    Guid RegistryId,
    int NumberingYear,
    long SequenceValue,
    string OfficialNumberText,
    int StatusId,
    Guid IssuedByUserId,
    DateTime IssuedAt,
    Guid? ReplacedByNumberId,
    string? CancellationReason,
    IReadOnlyCollection<DocumentNumberHistoryResponse> History)
{
    public static DocumentNumberResponse From(DocumentNumber number) => new(
        number.DocumentNumberId,
        number.DocumentId,
        number.RegistryId,
        number.NumberingYear,
        number.SequenceValue,
        number.OfficialNumberText,
        number.StatusId,
        number.IssuedByUserId,
        number.IssuedAt,
        number.ReplacedByNumberId,
        number.CancellationReason,
        number.History
            .OrderBy(history => history.OccurredAt)
            .Select(DocumentNumberHistoryResponse.From)
            .ToList());
}

/// <summary>One issued/cancelled/replaced event on an official number.</summary>
public sealed record DocumentNumberHistoryResponse(
    Guid HistoryId,
    int EventTypeId,
    Guid ActorUserId,
    string? Notes,
    DateTime OccurredAt)
{
    public static DocumentNumberHistoryResponse From(DocumentNumberHistory history) => new(
        history.HistoryId,
        history.EventTypeId,
        history.ActorUserId,
        history.Notes,
        history.OccurredAt);
}
