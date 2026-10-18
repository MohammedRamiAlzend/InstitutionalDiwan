using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly DiwanDbContext _context;

    public DepartmentRepository(DiwanDbContext context) => _context = context;

    public Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        _context.Departments.FirstOrDefaultAsync(
            department => department.DepartmentId == departmentId, cancellationToken);

    public Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Departments.FirstOrDefaultAsync(
            department => department.Code == normalized, cancellationToken);
    }

    public Task<bool> HasActiveChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        _context.Departments.AnyAsync(
            department => department.ParentDepartmentId == departmentId && department.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Departments
            .OrderBy(department => department.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        // The hierarchy is small enough to walk in memory; keep it a single round-trip.
        var links = await _context.Departments
            .Select(department => new { department.DepartmentId, department.ParentDepartmentId })
            .ToListAsync(cancellationToken);

        var descendants = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var link in links.Where(link => link.ParentDepartmentId == current))
            {
                if (descendants.Add(link.DepartmentId))
                    queue.Enqueue(link.DepartmentId);
            }
        }

        return descendants;
    }

    public void Add(Department department) => _context.Departments.Add(department);
}
