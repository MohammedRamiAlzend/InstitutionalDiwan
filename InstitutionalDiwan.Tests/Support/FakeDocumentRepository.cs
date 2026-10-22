using InstitutionalDiwan.Domain.Common;
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

    public Task<PagedList<Document>> ListPagedAsync(
        PageRequest page,
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Documents.AsEnumerable();

        if (statusId is { } status)
            query = query.Where(document => document.CurrentStatusId == status);

        if (owningDepartmentId is { } departmentId)
            query = query.Where(document => document.OwningDepartmentId == departmentId);

        if (directionId is { } direction)
            query = query.Where(document => document.DirectionId == direction);

        if (documentTypeId is { } type)
            query = query.Where(document => document.DocumentTypeId == type);

        if (page.NormalizedSearch is { } search)
            query = query.Where(document =>
                document.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (document.SubjectSummary?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (document.OfficialNumberText?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        query = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(document => document.Title),
            ("title", false) => query.OrderBy(document => document.Title),
            ("registrationdate", true) => query.OrderByDescending(document => document.RegistrationDate),
            ("registrationdate", false) => query.OrderBy(document => document.RegistrationDate),
            ("officialnumber", true) => query.OrderByDescending(document => document.OfficialNumberText),
            ("officialnumber", false) => query.OrderBy(document => document.OfficialNumberText),
            (_, true) => query.OrderByDescending(document => document.CreatedAt),
            _ => query.OrderBy(document => document.CreatedAt)
        };

        var totalCount = query.Count();
        var items = query.Skip(page.Skip).Take(page.Take).ToList();
        return Task.FromResult(new PagedList<Document>(items, totalCount));
    }

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
