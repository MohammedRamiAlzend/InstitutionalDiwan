using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListEmployeesQueryHandlerTests
{
    private static ListEmployeesQueryHandler CreateHandler(
        FakeEmployeeRepository employees,
        FakeDepartmentRepository? departments = null)
        => new(employees, departments ?? new FakeDepartmentRepository());

    [Fact]
    public async Task Handle_ReturnsEmployeesOrderedByName()
    {
        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Zaid", Guid.NewGuid()).Value);
        employees.Add(Employee.Create("Ali", Guid.NewGuid()).Value);

        var result = await CreateHandler(employees).Handle(
            new ListEmployeesQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Ali", "Zaid" }, result.Value.Items.Select(employee => employee.FullNameAr));
    }

    [Fact]
    public async Task Handle_WithNoEmployees_ReturnsEmptyPage()
    {
        var result = await CreateHandler(new FakeEmployeeRepository()).Handle(
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

        var result = await CreateHandler(employees).Handle(
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

        var result = await CreateHandler(employees).Handle(
            new ListEmployeesQuery(Search: "zaid"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var employee = Assert.Single(result.Value.Items);
        Assert.Equal("Zaid Omar", employee.FullNameAr);
    }

    [Fact]
    public async Task Handle_FiltersByTheExactDepartmentWhenSubDepartmentsAreExcluded()
    {
        var directorateId = Guid.NewGuid();
        var employeeDepartmentId = Guid.NewGuid();
        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Director", directorateId).Value);
        employees.Add(Employee.Create("Specialist", employeeDepartmentId).Value);

        var result = await CreateHandler(employees).Handle(
            new ListEmployeesQuery(DepartmentId: directorateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var employee = Assert.Single(result.Value.Items);
        Assert.Equal("Director", employee.FullNameAr);
    }

    [Fact]
    public async Task Handle_IncludesSubDepartmentsWhenAsked()
    {
        var departments = new FakeDepartmentRepository();
        departments.Add(Department.Create("DIR", "مديرية", 1).Value);
        var directorateId = departments.Departments[0].DepartmentId;
        departments.Add(Department.Create("DEP", "قسم", 2, parentDepartmentId: directorateId).Value);
        var departmentId = departments.Departments[1].DepartmentId;
        departments.Add(Department.Create("SEC", "شعبة", 3, parentDepartmentId: departmentId).Value);
        var sectionId = departments.Departments[2].DepartmentId;

        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Director", directorateId).Value);
        employees.Add(Employee.Create("Specialist", departmentId).Value);
        employees.Add(Employee.Create("Clerk", sectionId).Value);
        employees.Add(Employee.Create("Outsider", Guid.NewGuid()).Value);

        var result = await CreateHandler(employees, departments).Handle(
            new ListEmployeesQuery(DepartmentId: directorateId, IncludeSubDepartments: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { "Clerk", "Director", "Specialist" },
            result.Value.Items.Select(employee => employee.FullNameAr));
    }

    [Fact]
    public async Task Handle_WithUnknownDepartment_ReturnsAnEmptyPage()
    {
        var employees = new FakeEmployeeRepository();
        employees.Add(Employee.Create("Ali", Guid.NewGuid()).Value);

        var result = await CreateHandler(employees).Handle(
            new ListEmployeesQuery(DepartmentId: Guid.NewGuid(), IncludeSubDepartments: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }
}
