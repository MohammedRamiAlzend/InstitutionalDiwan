using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentRepository : IDocumentRepository
{
    public List<Document> Documents { get; } = new();

    public Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Documents.FirstOrDefault(document => document.DocumentId == documentId));

    public Task<IReadOnlyList<Document>> ListRecentAsync(int limit, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Document>>(
            Documents.OrderByDescending(document => document.CreatedAt).Take(limit).ToList());

    public void Add(Document document) => Documents.Add(document);
}
