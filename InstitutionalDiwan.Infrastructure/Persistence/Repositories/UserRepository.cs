using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly DiwanDbContext _context;

    public UserRepository(DiwanDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Query().OrderBy(user => user.Username).ToListAsync(cancellationToken);

    public async Task<PagedList<User>> ListPagedAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (page.NormalizedSearch is { } search)
            query = query.Where(user => user.Username.Contains(search));

        query = query.OrderBy(user => user.Username);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsSplitQuery()
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<User>(items, totalCount);
    }

    public void Add(User user) => _context.Users.Add(user);

    private IQueryable<User> Query() =>
        _context.Users
            .Include(user => user.UserRoles)
            .Include(user => user.DepartmentAssignments);
}
