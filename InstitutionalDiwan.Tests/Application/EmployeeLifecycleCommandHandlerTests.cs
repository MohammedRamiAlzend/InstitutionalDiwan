using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class EmployeeLifecycleCommandHandlerTests
{
    private static (FakeEmployeeRepository Employees, Employee Employee) Setup()
    {
        var employee = Employee.Create("أحمد علي", Guid.NewGuid());
        Assert.True(employee.IsSuccess);

        var employees = new FakeEmployeeRepository();
        employees.Add(employee.Value);

        return (employees, employee.Value);
    }

    [Fact]
    public async Task Deactivate_WithActiveEmployee_DeactivatesAndSaves()
    {
        var (employees, employee) = Setup();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new DeactivateEmployeeCommandHandler(employees, unitOfWork).Handle(
            new DeactivateEmployeeCommand(employee.EmployeeId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(employee.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Deactivate_WhenEmployeeMissing_ReturnsNotFound()
    {
        var result = await new DeactivateEmployeeCommandHandler(
            new FakeEmployeeRepository(), new FakeUnitOfWork()).Handle(
            new DeactivateEmployeeCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.EmployeeNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Activate_WithInactiveEmployee_ReactivatesAndSaves()
    {
        var (employees, employee) = Setup();
        Assert.True(employee.Deactivate().IsSuccess);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ActivateEmployeeCommandHandler(employees, unitOfWork).Handle(
            new ActivateEmployeeCommand(employee.EmployeeId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Activate_WhenEmployeeMissing_ReturnsNotFound()
    {
        var result = await new ActivateEmployeeCommandHandler(
            new FakeEmployeeRepository(), new FakeUnitOfWork()).Handle(
            new ActivateEmployeeCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.EmployeeNotFound.Code, result.Error.Code);
    }
}
