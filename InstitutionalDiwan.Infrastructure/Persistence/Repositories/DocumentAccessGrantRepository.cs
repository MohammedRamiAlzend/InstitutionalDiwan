using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentAccessGrantRepository : IDocumentAccessGrantRepository
{
    private readonly DiwanDbContext _context;

    public DocumentAccessGrantRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentAccessGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken = default) =>
        _context.DocumentAccessGrants.FirstOrDefaultAsync(grant => grant.GrantId == grantId, cancellationToken);

    public async Task<IReadOnlyList<DocumentAccessGrant>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentAccessGrants
            .Where(grant => grant.DocumentId == documentId)
            .OrderByDescending(grant => grant.GrantedAt)
            .ToListAsync(cancellationToken);

    public void Add(DocumentAccessGrant grant) => _context.DocumentAccessGrants.Add(grant);
}
