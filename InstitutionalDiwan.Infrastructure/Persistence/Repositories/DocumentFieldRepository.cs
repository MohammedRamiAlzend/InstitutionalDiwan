using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentFieldRepository : IDocumentFieldRepository
{
    private readonly DiwanDbContext _context;

    public DocumentFieldRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentField?> GetByIdAsync(Guid documentFieldId, CancellationToken cancellationToken = default) =>
        _context.DocumentFields.FirstOrDefaultAsync(field => field.DocumentFieldId == documentFieldId, cancellationToken);

    public Task<DocumentField?> GetByKeyAsync(int documentTypeId, string key, CancellationToken cancellationToken = default) =>
        _context.DocumentFields.FirstOrDefaultAsync(
            field => field.DocumentTypeId == documentTypeId && field.Key == key, cancellationToken);

    public async Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentFields
            .AsNoTracking()
            .Where(field => field.DocumentTypeId == documentTypeId)
            .OrderBy(field => field.CreatedAt)
            .ThenBy(field => field.Key)
            .ToListAsync(cancellationToken);

    public void Add(DocumentField field) => _context.DocumentFields.Add(field);
}
