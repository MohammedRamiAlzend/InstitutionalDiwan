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

    public Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(
                number => number.DocumentId == documentId && number.StatusId == DocumentNumberStatus.ActiveId,
                cancellationToken);

    public Task<bool> ExistsByNumberAndYearAsync(
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.OfficialNumberText == officialNumberText && number.NumberingYear == numberingYear,
            cancellationToken);

    public void Add(DocumentNumber documentNumber) => _context.DocumentNumbers.Add(documentNumber);
}
