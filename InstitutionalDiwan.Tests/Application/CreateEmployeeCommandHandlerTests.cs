using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateEmployeeCommandHandlerTests
{
    private static FakeDepartmentRepository DepartmentsWithOne()
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "الموارد البشرية", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        departments.Add(department.Value);
        return departments;
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesEmployeeAndSaves()
    {
        var employees = new FakeEmployeeRepository();
        var departments = DepartmentsWithOne();
        var unitOfWork = new FakeUnitOfWork();
        var departmentId = departments.Departments[0].DepartmentId;

        var result = await new CreateEmployeeCommandHandler(employees, departments, unitOfWork).Handle(
            new CreateEmployeeCommand("أحمد علي", departmentId, "EMP-001", "Ahmed Ali", "محاسب", "ahmed@example.com", "0770"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("أحمد علي", result.Value.FullNameAr);
        Assert.Equal("Ahmed Ali", result.Value.FullNameEn);
        Assert.Equal(departmentId, result.Value.DepartmentId);
        Assert.Equal("EMP-001", result.Value.EmployeeNumber);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = Assert.Single(employees.Employees);
        Assert.Equal(result.Value.EmployeeId, stored.EmployeeId);
    }

    [Fact]
    public async Task Handle_WhenDepartmentMissing_ReturnsNotFound()
    {
        var result = await new CreateEmployeeCommandHandler(
            new FakeEmployeeRepository(), new FakeDepartmentRepository(), new FakeUnitOfWork()).Handle(
            new CreateEmployeeCommand("أحمد علي", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WithBlankName_ReturnsValidationError()
    {
        var departments = DepartmentsWithOne();

        var result = await new CreateEmployeeCommandHandler(
            new FakeEmployeeRepository(), departments, new FakeUnitOfWork()).Handle(
            new CreateEmployeeCommand("   ", departments.Departments[0].DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.EmployeeNameEmpty.Code, result.Error.Code);
    }
}
