using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetDepartmentTreeQueryHandlerTests
{
    private const int TypeId = DepartmentType.SectionId;

    private static Department CreateDepartment(string code, Guid? parentId = null)
    {
        var department = Department.Create(code, "قسم", TypeId, parentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    private static async Task<IReadOnlyCollection<DepartmentTreeNodeResponse>> RunAsync(
        FakeDepartmentRepository departments) =>
        (await new GetDepartmentTreeQueryHandler(departments)
            .Handle(new GetDepartmentTreeQuery(), CancellationToken.None)).Value;

    [Fact]
    public async Task Handle_WithNoDepartments_ReturnsEmptyTree()
    {
        var tree = await RunAsync(new FakeDepartmentRepository());

        Assert.Empty(tree);
    }

    [Fact]
    public async Task Handle_BuildsNestedHierarchyFromRoots()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        var child = CreateDepartment("CHILD", root.DepartmentId);
        var grandChild = CreateDepartment("GRAND", child.DepartmentId);
        var otherRoot = CreateDepartment("OTHER");
        departments.Add(root);
        departments.Add(child);
        departments.Add(grandChild);
        departments.Add(otherRoot);

        var tree = await RunAsync(departments);

        Assert.Equal(2, tree.Count);
        Assert.Equal(new[] { "OTHER", "ROOT" }, tree.Select(node => node.Code));

        var rootNode = Assert.Single(tree, node => node.Code == "ROOT");
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal("CHILD", childNode.Code);
        Assert.Equal(root.DepartmentId, childNode.ParentDepartmentId);
        var grandChildNode = Assert.Single(childNode.Children);
        Assert.Equal("GRAND", grandChildNode.Code);
    }

    [Fact]
    public async Task Handle_OrdersSiblingsByCode()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        departments.Add(root);
        departments.Add(CreateDepartment("B", root.DepartmentId));
        departments.Add(CreateDepartment("A", root.DepartmentId));

        var tree = await RunAsync(departments);

        var rootNode = Assert.Single(tree);
        Assert.Equal(new[] { "A", "B" }, rootNode.Children.Select(node => node.Code));
    }

    [Fact]
    public async Task Handle_TreatsOrphansAsRoots()
    {
        var departments = new FakeDepartmentRepository();
        var orphan = CreateDepartment("ORPHAN", Guid.NewGuid());
        departments.Add(orphan);

        var tree = await RunAsync(departments);

        var node = Assert.Single(tree);
        Assert.Equal("ORPHAN", node.Code);
        Assert.Empty(node.Children);
    }
}
