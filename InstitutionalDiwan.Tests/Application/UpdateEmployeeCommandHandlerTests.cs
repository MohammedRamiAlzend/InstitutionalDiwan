using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UpdateEmployeeCommandHandlerTests
{
    private static (FakeEmployeeRepository Employees, FakeDepartmentRepository Departments, Employee Employee)
        Setup()
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "الموارد البشرية", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        departments.Add(department.Value);

        var employee = Employee.Create("أحمد علي", department.Value.DepartmentId, "EMP-001");
        Assert.True(employee.IsSuccess);

        var employees = new FakeEmployeeRepository();
        employees.Add(employee.Value);

        return (employees, departments, employee.Value);
    }

    [Fact]
    public async Task Handle_WithValidRequest_UpdatesEmployeeAndSaves()
    {
        var (employees, departments, employee) = Setup();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateEmployeeCommandHandler(employees, departments, unitOfWork).Handle(
            new UpdateEmployeeCommand(employee.EmployeeId, "محمد كريم", employee.DepartmentId, JobTitleAr: "مدير"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("محمد كريم", result.Value.FullNameAr);
        Assert.Equal("مدير", result.Value.JobTitleAr);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenEmployeeMissing_ReturnsNotFound()
    {
        var departments = new FakeDepartmentRepository();

        var result = await new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(), departments, new FakeUnitOfWork()).Handle(
            new UpdateEmployeeCommand(Guid.NewGuid(), "أحمد علي", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.EmployeeNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenDepartmentMissing_ReturnsNotFound()
    {
        var (employees, _, employee) = Setup();

        var result = await new UpdateEmployeeCommandHandler(
            employees, new FakeDepartmentRepository(), new FakeUnitOfWork()).Handle(
            new UpdateEmployeeCommand(employee.EmployeeId, "أحمد علي", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
    }
}
