using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Fields per form. A null department is the type's creation form, so key lookups used by registry
/// filters and columns never match a department's record field with the same key.
/// </summary>
internal sealed class DocumentFieldRepository : IDocumentFieldRepository
{
    private readonly DiwanDbContext _context;

    public DocumentFieldRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentField?> GetByIdAsync(Guid documentFieldId, CancellationToken cancellationToken = default) =>
        _context.DocumentFields.FirstOrDefaultAsync(field => field.DocumentFieldId == documentFieldId, cancellationToken);

    public Task<DocumentField?> GetByKeyAsync(int documentTypeId, string key, CancellationToken cancellationToken = default) =>
        GetByKeyAsync(documentTypeId, null, key, cancellationToken);

    public Task<DocumentField?> GetByKeyAsync(
        int documentTypeId, Guid? departmentId, string key, CancellationToken cancellationToken = default) =>
        OfForm(documentTypeId, departmentId).FirstOrDefaultAsync(field => field.Key == key, cancellationToken);

    public Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        CancellationToken cancellationToken = default) =>
        ListByDocumentTypeAsync(documentTypeId, null, cancellationToken);

    public async Task<IReadOnlyList<DocumentField>> ListByDocumentTypeAsync(
        int documentTypeId,
        Guid? departmentId,
        CancellationToken cancellationToken = default) =>
        await OfForm(documentTypeId, departmentId)
            .AsNoTracking()
            .OrderBy(field => field.CreatedAt)
            .ThenBy(field => field.Key)
            .ToListAsync(cancellationToken);

    public void Add(DocumentField field) => _context.DocumentFields.Add(field);

    private IQueryable<DocumentField> OfForm(int documentTypeId, Guid? departmentId) =>
        departmentId is { } department
            ? _context.DocumentFields.Where(field => field.DocumentTypeId == documentTypeId && field.DepartmentId == department)
            : _context.DocumentFields.Where(field => field.DocumentTypeId == documentTypeId && field.DepartmentId == null);
}
