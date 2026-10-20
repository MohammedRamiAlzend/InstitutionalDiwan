using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentNumberRepository : IDocumentNumberRepository
{
    private readonly DiwanDbContext _context;

    public DocumentNumberRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentNumber?> GetByIdAsync(Guid documentNumberId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(number => number.DocumentNumberId == documentNumberId, cancellationToken);

    /// <summary>
    /// The active <em>central</em> number. Department entries are separate lines and must never be
    /// picked up by the cancel/replace lifecycle.
    /// </summary>
    public Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(
                number => number.DocumentId == documentId &&
                          number.ScopeId == DocumentNumberScope.CentralId &&
                          number.StatusId == DocumentNumberStatus.ActiveId,
                cancellationToken);

    public Task<bool> ExistsByNumberAndYearAsync(
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.OfficialNumberText == officialNumberText && number.NumberingYear == numberingYear,
            cancellationToken);

    public Task<bool> ExistsInScopeAsync(
        int scopeId,
        Guid registryId,
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.ScopeId == scopeId &&
                      number.RegistryId == registryId &&
                      number.OfficialNumberText == officialNumberText &&
                      number.NumberingYear == numberingYear,
            cancellationToken);

    public Task<DocumentNumber?> GetDepartmentEntryAsync(
        Guid documentId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(
                number => number.DocumentId == documentId &&
                          number.ScopeId == DocumentNumberScope.DepartmentId &&
                          number.DepartmentId == departmentId,
                cancellationToken);

    public async Task<IReadOnlyList<DocumentNumber>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentNumbers
            .Include(number => number.History)
            .Where(number => number.DocumentId == documentId)
            .OrderBy(number => number.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentNumber>> ListByDepartmentAsync(
        Guid departmentId,
        int? numberingYear,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentNumbers
            .Include(number => number.History)
            .Where(number => number.DepartmentId == departmentId &&
                             (numberingYear == null || number.NumberingYear == numberingYear))
            .OrderBy(number => number.RegistryDate)
            .ThenBy(number => number.OfficialNumberText)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyByRegistryAsync(Guid registryId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.RegistryId == registryId &&
                      number.ScopeId == DocumentNumberScope.CentralId,
            cancellationToken);

    public void Add(DocumentNumber documentNumber) => _context.DocumentNumbers.Add(documentNumber);
}
