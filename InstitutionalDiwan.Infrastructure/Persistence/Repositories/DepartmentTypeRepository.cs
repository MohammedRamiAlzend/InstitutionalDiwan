using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentTypeRepository : IDepartmentTypeRepository
{
    private readonly DiwanDbContext _context;

    public DepartmentTypeRepository(DiwanDbContext context) => _context = context;

    public Task<DepartmentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.DepartmentTypes.FirstOrDefaultAsync(
            departmentType => departmentType.Code == normalized, cancellationToken);
    }

    public void Add(DepartmentType departmentType) => _context.DepartmentTypes.Add(departmentType);
}
