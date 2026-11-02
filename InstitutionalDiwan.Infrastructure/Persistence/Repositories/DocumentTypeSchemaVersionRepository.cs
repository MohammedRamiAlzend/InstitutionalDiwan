using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentTypeSchemaVersionRepository : IDocumentTypeSchemaVersionRepository
{
    private readonly DiwanDbContext _context;

    public DocumentTypeSchemaVersionRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentTypeSchemaVersion?> GetByIdAsync(Guid schemaVersionId, CancellationToken cancellationToken = default) =>
        _context.DocumentTypeSchemaVersions
            .Include(version => version.Fields)
            .FirstOrDefaultAsync(version => version.SchemaVersionId == schemaVersionId, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        _context.DocumentTypeSchemaVersions
            .Include(version => version.Fields)
            .FirstOrDefaultAsync(
                version => version.DocumentTypeId == documentTypeId
                    && version.Status == DocumentTypeSchemaVersionStatus.Draft,
                cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        _context.DocumentTypeSchemaVersions
            .Include(version => version.Fields)
            .Where(version => version.DocumentTypeId == documentTypeId
                && version.Status == DocumentTypeSchemaVersionStatus.Published)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetLatestVersionNumberAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        await _context.DocumentTypeSchemaVersions
            .Where(version => version.DocumentTypeId == documentTypeId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentTypeSchemaVersions
            .AsNoTracking()
            .Include(version => version.Fields)
            .Where(version => version.DocumentTypeId == documentTypeId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

    public void Add(DocumentTypeSchemaVersion version) => _context.DocumentTypeSchemaVersions.Add(version);

    public void Remove(DocumentTypeSchemaVersion version) => _context.DocumentTypeSchemaVersions.Remove(version);
}
