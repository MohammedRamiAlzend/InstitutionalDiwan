using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentRepository : IDocumentRepository
{
    private readonly DiwanDbContext _context;

    public DocumentRepository(DiwanDbContext context) => _context = context;

    public Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.Documents
            .Include(document => document.Participants)
            .Include(document => document.StatusHistory)
            .FirstOrDefaultAsync(document => document.DocumentId == documentId, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListRecentAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        await _context.Documents
            .OrderByDescending(document => document.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(Document document) => _context.Documents.Add(document);
}
