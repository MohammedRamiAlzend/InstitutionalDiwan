using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly DiwanDbContext _context;

    public PermissionRepository(DiwanDbContext context) => _context = context;

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Permissions.FirstOrDefaultAsync(
            permission => permission.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Permissions
            .OrderBy(permission => permission.ModuleName)
            .ThenBy(permission => permission.Code)
            .ToListAsync(cancellationToken);
}
