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
        Assert.Equal(new[] { "Ali", "Zaid" }, result.Value.Items.Select(employee => employee.FullNameAr));
    }

    [Fact]
    public async Task Handle_WithNoEmployees_ReturnsEmptyPage()
    {
        var result = await new ListEmployeesQueryHandler(new FakeEmployeeRepository()).Handle(
            new ListEmployeesQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_PagesEmployeesAndReportsTheTotal()
    {
        var employees = new FakeEmployeeRepository();
        for (var i = 0; i < 15; i++)
            employees.Add(Employee.Create($"Employee {i:00}", Guid.NewGuid()).Value);

        var result = await new ListEmployeesQueryHandler(employees).Handle(
            new ListEmployeesQuery(Page: 2, PageSize: 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Items.Count);
        Assert.Equal(15, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
    }

    [Fact]
    public async Task Handle_SearchesTheArabicAndEnglishNames()
    {
        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Ali Hassan", Guid.NewGuid(), fullNameEn: "Ali Hassan").Value);
        employees.Add(Employee.Create("Zaid Omar", Guid.NewGuid(), fullNameEn: "Zaid Omar").Value);

        var result = await new ListEmployeesQueryHandler(employees).Handle(
            new ListEmployeesQuery(Search: "zaid"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var employee = Assert.Single(result.Value.Items);
        Assert.Equal("Zaid Omar", employee.FullNameAr);
    }
}
