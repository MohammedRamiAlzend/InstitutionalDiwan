using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListEmployeesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmployeesOrderedByName()
    {
        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Zaid", Guid.NewGuid()).Value);
        employees.Add(Employee.Create("Ali", Guid.NewGuid()).Value);

        var result = await new ListEmployeesQueryHandler(employees).Handle(
            new ListEmployeesQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Ali", "Zaid" }, result.Value.Select(employee => employee.FullNameAr));
    }

    [Fact]
    public async Task Handle_WithNoEmployees_ReturnsEmpty()
    {
        var result = await new ListEmployeesQueryHandler(new FakeEmployeeRepository()).Handle(
            new ListEmployeesQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
