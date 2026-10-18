using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly DiwanDbContext _context;

    public RoleRepository(DiwanDbContext context) => _context = context;

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(role => role.RoleId == roleId, cancellationToken);

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(role => role.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Query().OrderBy(role => role.Code).ToListAsync(cancellationToken);

    public void Add(Role role) => _context.Roles.Add(role);

    private IQueryable<Role> Query() =>
        _context.Roles.Include(role => role.RolePermissions);
}
