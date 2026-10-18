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

    public void Add(Employee employee) => _context.Employees.Add(employee);
}
