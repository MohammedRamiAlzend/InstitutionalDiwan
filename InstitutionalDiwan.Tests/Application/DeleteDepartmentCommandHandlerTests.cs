using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DeleteDepartmentCommandHandlerTests
{
    private const string TypeCode = "SECTION";

    private static Department CreateDepartment(string code, Guid? parentId = null)
    {
        var department = Department.Create(code, "قسم", TypeCode, parentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    [Fact]
    public async Task Handle_WhenNoActiveChildren_DeactivatesAndSaves()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new DeleteDepartmentCommandHandler(departments, unitOfWork).Handle(
            new DeleteDepartmentCommand(department.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(department.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await new DeleteDepartmentCommandHandler(new FakeDepartmentRepository(), new FakeUnitOfWork())
            .Handle(new DeleteDepartmentCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenAnActiveChildExists_ReturnsConflict()
    {
        var departments = new FakeDepartmentRepository();
        var parent = CreateDepartment("ROOT");
        departments.Add(parent);
        departments.Add(CreateDepartment("CHILD", parent.DepartmentId));

        var result = await new DeleteDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new DeleteDepartmentCommand(parent.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.HasActiveChildren.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenChildrenAreInactive_Deactivates()
    {
        var departments = new FakeDepartmentRepository();
        var parent = CreateDepartment("ROOT");
        var child = CreateDepartment("CHILD", parent.DepartmentId);
        child.Deactivate(hasActiveChildren: false);
        departments.Add(parent);
        departments.Add(child);

        var result = await new DeleteDepartmentCommandHandler(departments, new FakeUnitOfWork()).Handle(
            new DeleteDepartmentCommand(parent.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(parent.IsActive);
    }
}
