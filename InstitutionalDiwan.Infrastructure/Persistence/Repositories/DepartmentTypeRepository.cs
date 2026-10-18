using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentTypeRepository : IDepartmentTypeRepository
{
    private readonly DiwanDbContext _context;

    public DepartmentTypeRepository(DiwanDbContext context) => _context = context;

    public Task<DepartmentType?> GetByIdAsync(int departmentTypeId, CancellationToken cancellationToken = default) =>
        _context.DepartmentTypes.FirstOrDefaultAsync(
            departmentType => departmentType.Id == departmentTypeId, cancellationToken);

    public async Task<IReadOnlyList<DepartmentType>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.DepartmentTypes
            .OrderBy(departmentType => departmentType.HierarchyLevelOrder)
            .ToListAsync(cancellationToken);

    public void Add(DepartmentType departmentType) => _context.DepartmentTypes.Add(departmentType);
}
