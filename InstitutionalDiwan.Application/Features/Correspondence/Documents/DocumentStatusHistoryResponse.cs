using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>One recorded document status transition.</summary>
public sealed record DocumentStatusHistoryResponse(
    Guid HistoryId,
    int? FromStatusId,
    int ToStatusId,
    Guid ChangedByUserId,
    string? Reason,
    DateTime ChangedAt)
{
    public static DocumentStatusHistoryResponse From(DocumentStatusHistory history) => new(
        history.HistoryId,
        history.FromStatusId,
        history.ToStatusId,
        history.ChangedByUserId,
        history.Reason,
        history.ChangedAt);
}
