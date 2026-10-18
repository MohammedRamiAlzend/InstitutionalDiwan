using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class MoveDepartmentCommandHandlerTests
{
    private const string TypeCode = "SECTION";

    private static Department CreateDepartment(string code, Guid? parentId = null)
    {
        var department = Department.Create(code, "قسم", TypeCode, parentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    [Fact]
    public async Task Handle_MovesDepartmentUnderNewParent()
    {
        var departments = new FakeDepartmentRepository();
        var target = CreateDepartment("TARGET");
        var department = CreateDepartment("HR");
        departments.Add(target);
        departments.Add(department);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new MoveDepartmentCommandHandler(departments, unitOfWork).Handle(
            new MoveDepartmentCommand(department.DepartmentId, target.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(target.DepartmentId, result.Value.ParentDepartmentId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithNullParent_PromotesToRoot()
    {
        var departments = new FakeDepartmentRepository();
        var parent = CreateDepartment("ROOT");
        var department = CreateDepartment("HR", parent.DepartmentId);
        departments.Add(parent);
        departments.Add(department);

        var result = await new MoveDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new MoveDepartmentCommand(department.DepartmentId, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ParentDepartmentId);
    }

    [Fact]
    public async Task Handle_WhenParentIsUnchanged_ReturnsWithoutSaving()
    {
        var departments = new FakeDepartmentRepository();
        var parent = CreateDepartment("ROOT");
        var department = CreateDepartment("HR", parent.DepartmentId);
        departments.Add(parent);
        departments.Add(department);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new MoveDepartmentCommandHandler(departments, unitOfWork).Handle(
            new MoveDepartmentCommand(department.DepartmentId, parent.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(parent.DepartmentId, result.Value.ParentDepartmentId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await new MoveDepartmentCommandHandler(new FakeDepartmentRepository(), new FakeUnitOfWork())
            .Handle(new MoveDepartmentCommand(Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNewParentIsUnknown_ReturnsValidationError()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);

        var result = await new MoveDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new MoveDepartmentCommand(department.DepartmentId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.ParentNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNewParentIsOwnDescendant_ReturnsCycleConflict()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        var child = CreateDepartment("CHILD", root.DepartmentId);
        var grandChild = CreateDepartment("GRAND", child.DepartmentId);
        departments.Add(root);
        departments.Add(child);
        departments.Add(grandChild);

        var result = await new MoveDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new MoveDepartmentCommand(root.DepartmentId, grandChild.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.CyclicHierarchy.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNewParentIsItself_ReturnsSelfParentConflict()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);

        var result = await new MoveDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new MoveDepartmentCommand(department.DepartmentId, department.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.SelfParent.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNewParentIsInactive_ReturnsConflict()
    {
        var departments = new FakeDepartmentRepository();
        var parent = CreateDepartment("ROOT");
        parent.Deactivate(hasActiveChildren: false);
        var department = CreateDepartment("HR");
        departments.Add(parent);
        departments.Add(department);

        var result = await new MoveDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new MoveDepartmentCommand(department.DepartmentId, parent.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.InactiveParent.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
