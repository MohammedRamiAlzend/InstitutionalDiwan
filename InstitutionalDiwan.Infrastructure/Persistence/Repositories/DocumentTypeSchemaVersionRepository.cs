using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Schema versions per form. A null department is the type's creation form, so the overloads without
/// a department never see a department's record form.
/// </summary>
internal sealed class DocumentTypeSchemaVersionRepository : IDocumentTypeSchemaVersionRepository
{
    private readonly DiwanDbContext _context;

    public DocumentTypeSchemaVersionRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentTypeSchemaVersion?> GetByIdAsync(Guid schemaVersionId, CancellationToken cancellationToken = default) =>
        _context.DocumentTypeSchemaVersions
            .Include(version => version.Fields)
            .FirstOrDefaultAsync(version => version.SchemaVersionId == schemaVersionId, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetDraftAsync(documentTypeId, null, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetDraftAsync(
        int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        OfForm(documentTypeId, departmentId)
            .Include(version => version.Fields)
            .FirstOrDefaultAsync(version => version.Status == DocumentTypeSchemaVersionStatus.Draft, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetPublishedAsync(documentTypeId, null, cancellationToken);

    public Task<DocumentTypeSchemaVersion?> GetPublishedAsync(
        int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        OfForm(documentTypeId, departmentId)
            .Include(version => version.Fields)
            .Where(version => version.Status == DocumentTypeSchemaVersionStatus.Published)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> GetLatestVersionNumberAsync(int documentTypeId, CancellationToken cancellationToken = default) =>
        GetLatestVersionNumberAsync(documentTypeId, null, cancellationToken);

    public async Task<int> GetLatestVersionNumberAsync(
        int documentTypeId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        await OfForm(documentTypeId, departmentId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

    public Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        ListByDocumentTypeAsync(documentTypeId, null, cancellationToken);

    public async Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListByDocumentTypeAsync(
        int documentTypeId,
        Guid? departmentId,
        CancellationToken cancellationToken = default) =>
        await OfForm(documentTypeId, departmentId)
            .AsNoTracking()
            .Include(version => version.Fields)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentTypeSchemaVersion>> ListIssuedByDepartmentAsync(
        Guid departmentId, CancellationToken cancellationToken = default) =>
        await _context.DocumentTypeSchemaVersions
            .AsNoTracking()
            .Include(version => version.Fields)
            .Where(version => version.DepartmentId == departmentId &&
                              version.Status != DocumentTypeSchemaVersionStatus.Draft)
            .OrderBy(version => version.DocumentTypeId)
            .ThenByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

    public void Add(DocumentTypeSchemaVersion version) => _context.DocumentTypeSchemaVersions.Add(version);

    public void Remove(DocumentTypeSchemaVersion version) => _context.DocumentTypeSchemaVersions.Remove(version);

    private IQueryable<DocumentTypeSchemaVersion> OfForm(int documentTypeId, Guid? departmentId) =>
        departmentId is { } department
            ? _context.DocumentTypeSchemaVersions.Where(version =>
                version.DocumentTypeId == documentTypeId && version.DepartmentId == department)
            : _context.DocumentTypeSchemaVersions.Where(version =>
                version.DocumentTypeId == documentTypeId && version.DepartmentId == null);
}
