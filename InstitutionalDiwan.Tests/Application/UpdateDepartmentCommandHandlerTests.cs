using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UpdateDepartmentCommandHandlerTests
{
    private const string TypeCode = "SECTION";

    private static Department CreateDepartment(string code, Guid? parentId = null)
    {
        var department = Department.Create(code, "قسم", TypeCode, parentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    [Fact]
    public async Task Handle_RenamesAndUpdatesExternalPolicy()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateDepartmentCommandHandler(departments, unitOfWork).Handle(
            new UpdateDepartmentCommand(
                department.DepartmentId,
                "الموارد البشرية",
                ParentDepartmentId: null,
                CanSendExternal: true,
                CanReceiveExternal: true,
                NameEn: "Human Resources"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("الموارد البشرية", result.Value.NameAr);
        Assert.Equal("Human Resources", result.Value.NameEn);
        Assert.True(result.Value.CanSendExternal);
        Assert.True(result.Value.CanReceiveExternal);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_MovesDepartmentUnderNewParent()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        var department = CreateDepartment("HR");
        departments.Add(root);
        departments.Add(department);

        var result = await new UpdateDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", root.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(root.DepartmentId, result.Value.ParentDepartmentId);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await new UpdateDepartmentCommandHandler(new FakeDepartmentRepository(), new FakeUnitOfWork())
            .Handle(new UpdateDepartmentCommand(Guid.NewGuid(), "قسم", null, false, false), CancellationToken.None);

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

        var result = await new UpdateDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", Guid.NewGuid(), false, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.ParentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenMovingUnderOwnDescendant_ReturnsCycleConflict()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        var child = CreateDepartment("CHILD", root.DepartmentId);
        var grandChild = CreateDepartment("GRAND", child.DepartmentId);
        departments.Add(root);
        departments.Add(child);
        departments.Add(grandChild);

        var result = await new UpdateDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new UpdateDepartmentCommand(root.DepartmentId, "قسم", grandChild.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.CyclicHierarchy.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenMakingDepartmentItsOwnParent_ReturnsSelfParentConflict()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);

        var result = await new UpdateDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", department.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.SelfParent.Code, result.Error.Code);
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

        var result = await new UpdateDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", parent.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.InactiveParent.Code, result.Error.Code);
    }
}
