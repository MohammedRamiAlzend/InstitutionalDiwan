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
        int? documentTypeId,
        int? directionId,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _context.NumberingRegistries
            .Include(registry => registry.Counters)
            .Where(registry => registry.IsActive &&
                               (registry.DepartmentId == null || registry.DepartmentId == departmentId) &&
                               (registry.DocumentTypeId == null || registry.DocumentTypeId == documentTypeId) &&
                               (registry.DirectionId == null || registry.DirectionId == directionId))
            .ToListAsync(cancellationToken);

        return candidates
            .OrderBy(registry => Specificity(registry, departmentId, documentTypeId, directionId))
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault();
    }

    public async Task<NumberingRegistry?> GetActiveDepartmentRegistryAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (departmentId == Guid.Empty)
            return null;

        var candidates = await _context.NumberingRegistries
            .Include(registry => registry.Counters)
            .Where(registry => registry.IsActive && registry.DepartmentId == departmentId)
            .ToListAsync(cancellationToken);

        return candidates
            .OrderByDescending(registry =>
                (registry.DocumentTypeId is null ? 0 : 1) + (registry.DirectionId is null ? 0 : 1))
            .ThenBy(registry => registry.CreatedAt)
            .FirstOrDefault();
    }

    public void Add(NumberingRegistry registry) => _context.NumberingRegistries.Add(registry);

    private static int Specificity(
        NumberingRegistry registry,
        Guid? departmentId,
        int? documentTypeId,
        int? directionId)
    {
        var specificity = 0;
        if (registry.DepartmentId is not null && registry.DepartmentId == departmentId)
            specificity++;
        if (registry.DocumentTypeId is not null && registry.DocumentTypeId == documentTypeId)
            specificity++;
        if (registry.DirectionId is not null && registry.DirectionId == directionId)
            specificity++;
        return specificity;
    }
}
