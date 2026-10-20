using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentRepository : IDocumentRepository
{
    public List<Document> Documents { get; } = new();

    public Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Documents.FirstOrDefault(document => document.DocumentId == documentId));

    public Task<Document?> GetWithNumbersAsync(Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Documents.FirstOrDefault(document => document.DocumentId == documentId));

    public Task<IReadOnlyList<Document>> ListRecentAsync(int limit, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Document>>(
            Documents.OrderByDescending(document => document.CreatedAt).Take(limit).ToList());

    public Task<IReadOnlyList<Document>> GetByIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Document>>(
            Documents.Where(document => documentIds.Contains(document.DocumentId)).ToList());

    public void Add(Document document) => Documents.Add(document);
}

internal sealed class FakeDocumentTypeRepository : IDocumentTypeRepository
{
    public List<DocumentType> DocumentTypes { get; } = new();

    public Task<DocumentType?> GetByIdAsync(int documentTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentTypes.FirstOrDefault(type => type.Id == documentTypeId));

    public Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentType>>(
            DocumentTypes.OrderBy(type => type.Id).ToList());

    public void Add(DocumentType documentType) => DocumentTypes.Add(documentType);

    public static FakeDocumentTypeRepository WithDefaultTypes()
    {
        var repository = new FakeDocumentTypeRepository();
        repository.DocumentTypes.AddRange(new[]
        {
            DocumentType.Create(DocumentType.IncomingLetterId, "Incoming letter", "كتاب وارد", DocumentDirection.IncomingId).Value,
            DocumentType.Create(DocumentType.OutgoingLetterId, "Outgoing letter", "كتاب صادر", DocumentDirection.OutgoingId).Value,
            DocumentType.Create(DocumentType.InternalMemoId, "Internal memo", "مذكرة داخلية", DocumentDirection.InternalId, requiresOfficialNumber: false).Value,
            DocumentType.Create(DocumentType.CircularId, "Circular", "تعميم", DocumentDirection.AllId).Value,
            DocumentType.Create(DocumentType.DecisionId, "Decision", "قرار", DocumentDirection.AllId).Value,
        });
        return repository;
    }
}
