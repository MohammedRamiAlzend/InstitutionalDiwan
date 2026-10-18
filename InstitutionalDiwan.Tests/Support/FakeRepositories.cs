using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = new();

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(user => user.UserId == userId));

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(user => user.Username == username));

    public Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<User>>(Users.OrderBy(user => user.Username).ToList());

    public void Add(User user) => Users.Add(user);
}

internal sealed class FakeRoleRepository : IRoleRepository
{
    public List<Role> Roles { get; } = new();

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        => Task.FromResult(Roles.FirstOrDefault(role => role.RoleId == roleId));

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(Roles.FirstOrDefault(role => role.Code == code));

    public Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Role>>(Roles.OrderBy(role => role.Code).ToList());

    public void Add(Role role) => Roles.Add(role);
}

internal sealed class FakePermissionRepository : IPermissionRepository
{
    public List<Permission> Permissions { get; } = new();

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return Task.FromResult(Permissions.FirstOrDefault(permission => permission.Code == normalized));
    }

    public Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Permission>>(
            Permissions.OrderBy(permission => permission.ModuleName)
                .ThenBy(permission => permission.Code)
                .ToList());
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}
