using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

/// <summary>Hierarchical read model returned by the department tree endpoint.</summary>
public sealed record DepartmentTreeNodeResponse(
    Guid DepartmentId,
    string Code,
    string NameAr,
    string? NameEn,
    int DepartmentTypeId,
    Guid? ParentDepartmentId,
    bool IsActive,
    IReadOnlyCollection<DepartmentTreeNodeResponse> Children)
{
    /// <summary>
    /// Builds the hierarchy from a flat department list. Departments whose parent is missing from
    /// the list are treated as roots so no node is silently dropped.
    /// </summary>
    public static IReadOnlyCollection<DepartmentTreeNodeResponse> BuildTree(
        IReadOnlyCollection<Department> departments)
    {
        var ids = departments.Select(department => department.DepartmentId).ToHashSet();
        var childrenByParent = new Dictionary<Guid, List<Department>>();
        var roots = new List<Department>();

        foreach (var department in departments)
        {
            if (department.ParentDepartmentId is { } parentId && ids.Contains(parentId))
            {
                if (!childrenByParent.TryGetValue(parentId, out var children))
                {
                    children = new List<Department>();
                    childrenByParent[parentId] = children;
                }

                children.Add(department);
            }
            else
            {
                roots.Add(department);
            }
        }

        IReadOnlyCollection<DepartmentTreeNodeResponse> Build(Guid? parentId) =>
            parentId is { } id && childrenByParent.TryGetValue(id, out var children)
                ? children
                    .OrderBy(child => child.Code)
                    .Select(child => From(child, Build(child.DepartmentId)))
                    .ToList()
                : Array.Empty<DepartmentTreeNodeResponse>();

        return roots
            .OrderBy(root => root.Code)
            .Select(root => From(root, Build(root.DepartmentId)))
            .ToList();
    }

    private static DepartmentTreeNodeResponse From(
        Department department,
        IReadOnlyCollection<DepartmentTreeNodeResponse> children) => new(
        department.DepartmentId,
        department.Code,
        department.NameAr,
        department.NameEn,
        department.DepartmentTypeId,
        department.ParentDepartmentId,
        department.IsActive,
        children);
}
