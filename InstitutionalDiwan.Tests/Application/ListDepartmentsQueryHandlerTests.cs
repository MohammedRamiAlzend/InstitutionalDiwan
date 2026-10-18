using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListDepartmentsQueryHandlerTests
{
    private const int TypeId = DepartmentType.SectionId;

    private static Department CreateDepartment(string code, string nameAr = "قسم")
    {
        var department = Department.Create(code, nameAr, TypeId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    [Fact]
    public async Task Handle_WithNoDepartments_ReturnsEmptyCollection()
    {
        var handler = new ListDepartmentsQueryHandler(new FakeDepartmentRepository());

        var result = await handler.Handle(new ListDepartmentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsDepartmentsOrderedByCode()
    {
        var departments = new FakeDepartmentRepository();
        departments.Add(CreateDepartment("SECTION_B"));
        departments.Add(CreateDepartment("SECTION_A"));

        var result = await new ListDepartmentsQueryHandler(departments)
            .Handle(new ListDepartmentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(new[] { "SECTION_A", "SECTION_B" }, result.Value.Select(department => department.Code));
    }
}

public sealed class GetDepartmentByIdQueryHandlerTests
{
    private const int TypeId = DepartmentType.SectionId;

    [Fact]
    public async Task Handle_WhenDepartmentExists_ReturnsMappedResponse()
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "الموارد البشرية", TypeId, nameEn: "Human Resources");
        Assert.True(department.IsSuccess);
        departments.Add(department.Value);

        var result = await new GetDepartmentByIdQueryHandler(departments)
            .Handle(new GetDepartmentByIdQuery(department.Value.DepartmentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(department.Value.DepartmentId, result.Value.DepartmentId);
        Assert.Equal("HR", result.Value.Code);
        Assert.Equal("الموارد البشرية", result.Value.NameAr);
        Assert.Equal("Human Resources", result.Value.NameEn);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await new GetDepartmentByIdQueryHandler(new FakeDepartmentRepository())
            .Handle(new GetDepartmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
