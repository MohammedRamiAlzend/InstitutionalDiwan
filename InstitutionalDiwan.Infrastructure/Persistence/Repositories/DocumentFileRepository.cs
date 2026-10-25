using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentFileRepository : IDocumentFileRepository
{
    private readonly DiwanDbContext _context;

    public DocumentFileRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentFile?> GetByIdAsync(
        Guid fileId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentFiles
            .Include(file => file.Versions)
            .FirstOrDefaultAsync(file => file.FileId == fileId, cancellationToken);

    public async Task<IReadOnlyList<DocumentFile>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentFiles
            .Include(file => file.Versions)
            .Where(file => file.DocumentId == documentId)
            .OrderBy(file => file.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(DocumentFile documentFile) => _context.DocumentFiles.Add(documentFile);
}

internal sealed class DocumentFileTypeRepository : IDocumentFileTypeRepository
{
    private readonly DiwanDbContext _context;

    public DocumentFileTypeRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentFileType?> GetByIdAsync(
        int fileTypeId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentFileTypes
            .FirstOrDefaultAsync(type => type.Id == fileTypeId, cancellationToken);
}

internal sealed class FileStorageProviderRepository : IFileStorageProviderRepository
{
    private readonly DiwanDbContext _context;

    public FileStorageProviderRepository(DiwanDbContext context) => _context = context;

    public Task<FileStorageProvider?> GetByIdAsync(
        int storageProviderId,
        CancellationToken cancellationToken = default) =>
        _context.FileStorageProviders
            .FirstOrDefaultAsync(provider => provider.Id == storageProviderId, cancellationToken);

    public Task<FileStorageProvider?> GetDefaultAsync(
        CancellationToken cancellationToken = default) =>
        _context.FileStorageProviders
            .Where(provider => provider.IsActive)
            .OrderByDescending(provider => provider.IsDefault)
            .ThenBy(provider => provider.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
