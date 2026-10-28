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

    public Task<PagedList<User>> ListPagedAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Users.AsEnumerable();

        if (page.NormalizedSearch is { } search)
            query = query.Where(user =>
                user.Username.Contains(search, StringComparison.OrdinalIgnoreCase));

        query = query.OrderBy(user => user.Username);

        var totalCount = query.Count();
        var items = query.Skip(page.Skip).Take(page.Take).ToList();
        return Task.FromResult(new PagedList<User>(items, totalCount));
    }

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

internal sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Employees { get; } = new();

    public Task<Employee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Employees.FirstOrDefault(employee => employee.EmployeeId == employeeId));

    public Task<IReadOnlyList<Employee>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Employee>>(
            Employees.OrderBy(employee => employee.FullNameAr).ToList());

    public Task<PagedList<Employee>> ListPagedAsync(
        PageRequest page,
        IReadOnlyCollection<Guid>? departmentIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = Employees.AsEnumerable();

        if (departmentIds is not null)
            query = query.Where(employee => departmentIds.Contains(employee.DepartmentId));

        if (page.NormalizedSearch is { } search)
            query = query.Where(employee =>
                employee.FullNameAr.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (employee.FullNameEn?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (employee.EmployeeNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        query = query.OrderBy(employee => employee.FullNameAr);

        var totalCount = query.Count();
        var items = query.Skip(page.Skip).Take(page.Take).ToList();
        return Task.FromResult(new PagedList<Employee>(items, totalCount));
    }

    public void Add(Employee employee) => Employees.Add(employee);
}

internal sealed class FakeDocumentAccessGrantRepository : IDocumentAccessGrantRepository
{
    public List<DocumentAccessGrant> Grants { get; } = new();

    public Task<DocumentAccessGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Grants.FirstOrDefault(grant => grant.GrantId == grantId));

    public Task<IReadOnlyList<DocumentAccessGrant>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentAccessGrant>>(
            Grants.Where(grant => grant.DocumentId == documentId)
                .OrderByDescending(grant => grant.GrantedAt)
                .ToList());

    public void Add(DocumentAccessGrant grant) => Grants.Add(grant);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    /// <summary>The instant the fake stamps its writes with; tests override it to freeze time.</summary>
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
