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

    public void Add(User user) => _context.Users.Add(user);

    private IQueryable<User> Query() =>
        _context.Users
            .Include(user => user.UserRoles)
            .Include(user => user.DepartmentAssignments);
}
