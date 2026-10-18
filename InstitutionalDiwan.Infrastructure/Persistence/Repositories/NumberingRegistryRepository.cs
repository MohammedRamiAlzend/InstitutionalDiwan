using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class NumberingRegistryRepository : INumberingRegistryRepository
{
    private readonly DiwanDbContext _context;

    public NumberingRegistryRepository(DiwanDbContext context) => _context = context;

    public Task<NumberingRegistry?> GetByIdAsync(Guid registryId, CancellationToken cancellationToken = default) =>
        _context.NumberingRegistries
            .Include(registry => registry.Counters)
            .FirstOrDefaultAsync(registry => registry.RegistryId == registryId, cancellationToken);

    public async Task<IReadOnlyList<NumberingRegistry>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.NumberingRegistries
            .Include(registry => registry.Counters)
            .OrderBy(registry => registry.Code)
            .ToListAsync(cancellationToken);

    public async Task<NumberingRegistry?> GetActiveForScopeAsync(
        Guid? departmentId,
        string? documentTypeCode,
        string? direction,
        CancellationToken cancellationToken = default)
    {
        var normalizedDocumentType = string.IsNullOrWhiteSpace(documentTypeCode)
            ? null
            : documentTypeCode.Trim().ToUpperInvariant();
        var normalizedDirection = string.IsNullOrWhiteSpace(direction)
            ? null
            : direction.Trim().ToUpperInvariant();

        var candidates = await _context.NumberingRegistries
            .Include(registry => registry.Counters)
            .Where(registry => registry.IsActive &&
                               (registry.DepartmentId == null || registry.DepartmentId == departmentId) &&
                               (registry.DocumentTypeCode == null || registry.DocumentTypeCode == normalizedDocumentType) &&
                               (registry.Direction == null || registry.Direction == normalizedDirection))
            .ToListAsync(cancellationToken);

        return candidates
            .OrderBy(registry => Specificity(registry, departmentId, normalizedDocumentType, normalizedDirection))
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault();
    }

    public void Add(NumberingRegistry registry) => _context.NumberingRegistries.Add(registry);

    private static int Specificity(
        NumberingRegistry registry,
        Guid? departmentId,
        string? documentTypeCode,
        string? direction)
    {
        var specificity = 0;
        if (registry.DepartmentId is not null && registry.DepartmentId == departmentId)
            specificity++;
        if (registry.DocumentTypeCode is not null && registry.DocumentTypeCode == documentTypeCode)
            specificity++;
        if (registry.Direction is not null && registry.Direction == direction)
            specificity++;
        return specificity;
    }
}
