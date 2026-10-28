using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly DiwanDbContext _context;

    public EmployeeRepository(DiwanDbContext context) => _context = context;

    public Task<Employee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        _context.Employees.FirstOrDefaultAsync(employee => employee.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<Employee>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Employees.OrderBy(employee => employee.FullNameAr).ToListAsync(cancellationToken);

    public async Task<PagedList<Employee>> ListPagedAsync(
        PageRequest page,
        IReadOnlyCollection<Guid>? departmentIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.AsNoTracking();

        if (departmentIds is not null)
        {
            if (departmentIds.Count == 0)
                return new PagedList<Employee>(Array.Empty<Employee>(), 0);

            query = query.Where(employee => departmentIds.Contains(employee.DepartmentId));
        }

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(employee =>
                EF.Functions.Like(employee.FullNameAr, pattern) ||
                (employee.FullNameEn != null && EF.Functions.Like(employee.FullNameEn, pattern)) ||
                (employee.EmployeeNumber != null && EF.Functions.Like(employee.EmployeeNumber, pattern)));
        }

        var ordered = (page.SortBy?.ToLowerInvariant(), page.SortDescending) switch
        {
            ("employeenumber", false) => query.OrderBy(employee => employee.EmployeeNumber),
            ("employeenumber", true) => query.OrderByDescending(employee => employee.EmployeeNumber),
            ("createdat", false) => query.OrderBy(employee => employee.CreatedAt),
            ("createdat", true) => query.OrderByDescending(employee => employee.CreatedAt),
            ("isactive", false) => query.OrderBy(employee => employee.IsActive),
            ("isactive", true) => query.OrderByDescending(employee => employee.IsActive),
            (_, true) => query.OrderByDescending(employee => employee.FullNameAr),
            _ => query.OrderBy(employee => employee.FullNameAr)
        };
        query = ordered.ThenBy(employee => employee.EmployeeId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<Employee>(items, totalCount);
    }

    public void Add(Employee employee) => _context.Employees.Add(employee);
}
