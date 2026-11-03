using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>An archive search row: the document plus who archived it, when, and where it is kept.</summary>
public sealed record ArchivedDocumentResponse(
    DocumentResponse Document,
    DateTime ArchivedAt,
    Guid ArchivedByUserId,
    int? ArchiveLocationId)
{
    public static ArchivedDocumentResponse From(ArchivedDocumentRow row) => new(
        DocumentResponse.From(row.Document),
        row.ArchivedAt,
        row.ArchivedByUserId,
        row.ArchiveLocationId);
}
