using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly DiwanDbContext _context;

    public DocumentTypeRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentType?> GetByIdAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        _context.DocumentTypes.FirstOrDefaultAsync(
            documentType => documentType.Id == documentTypeId, cancellationToken);

    public async Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.DocumentTypes
            .OrderBy(documentType => documentType.Id)
            .ToListAsync(cancellationToken);

    public void Add(DocumentType documentType) => _context.DocumentTypes.Add(documentType);
}
