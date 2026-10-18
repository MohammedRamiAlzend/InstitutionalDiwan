using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDepartmentRepository : IDepartmentRepository
{
    public List<Department> Departments { get; } = new();

    public Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Departments.FirstOrDefault(department => department.DepartmentId == departmentId));

    public Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return Task.FromResult(Departments.FirstOrDefault(department => department.Code == normalized));
    }

    public Task<bool> HasActiveChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Departments.Any(
            department => department.ParentDepartmentId == departmentId && department.IsActive));

    public Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Department>>(
            Departments.OrderBy(department => department.Code).ToList());

    public Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var descendants = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in Departments.Where(department => department.ParentDepartmentId == current))
            {
                if (descendants.Add(child.DepartmentId))
                    queue.Enqueue(child.DepartmentId);
            }
        }

        return Task.FromResult<IReadOnlySet<Guid>>(descendants);
    }

    public void Add(Department department) => Departments.Add(department);
}

internal sealed class FakeDepartmentTypeRepository : IDepartmentTypeRepository
{
    public List<DepartmentType> DepartmentTypes { get; } = new();

    public Task<DepartmentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return Task.FromResult(DepartmentTypes.FirstOrDefault(type => type.Code == normalized));
    }

    public void Add(DepartmentType departmentType) => DepartmentTypes.Add(departmentType);
}
