using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDepartmentCommandHandlerTests
{
    private const string TypeCode = "SECTION";

    private static FakeDepartmentTypeRepository ActiveTypes(bool active = true)
    {
        var types = new FakeDepartmentTypeRepository();
        var type = DepartmentType.Create(TypeCode, "قسم", hierarchyLevelOrder: 2);
        Assert.True(type.IsSuccess);
        if (!active)
            type.Value.Deactivate();
        types.Add(type.Value);
        return types;
    }

    private static CreateDepartmentCommandHandler CreateHandler(
        FakeDepartmentRepository departments,
        FakeDepartmentTypeRepository types,
        FakeUnitOfWork unitOfWork)
        => new(departments, types, unitOfWork);

    [Fact]
    public async Task Handle_WithValidRequest_CreatesDepartmentAndSaves()
    {
        var departments = new FakeDepartmentRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(departments, ActiveTypes(), unitOfWork).Handle(
            new CreateDepartmentCommand("hr", "الموارد البشرية", TypeCode, CanSendExternal: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("HR", result.Value.Code);
        Assert.Equal("الموارد البشرية", result.Value.NameAr);
        Assert.Equal(TypeCode, result.Value.DepartmentTypeCode);
        Assert.True(result.Value.IsActive);
        Assert.True(result.Value.CanSendExternal);
        Assert.Null(result.Value.ParentDepartmentId);
        Assert.Single(departments.Departments);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsConflict()
    {
        var departments = new FakeDepartmentRepository();
        var existing = Department.Create("HR", "الموارد البشرية", TypeCode);
        Assert.True(existing.IsSuccess);
        departments.Add(existing.Value);

        var result = await CreateHandler(departments, ActiveTypes(), new FakeUnitOfWork()).Handle(
            new CreateDepartmentCommand("hr", "إدارة أخرى", TypeCode),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.CodeAlreadyExists.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenTypeIsUnknown_ReturnsValidationError()
    {
        var result = await CreateHandler(new FakeDepartmentRepository(), new FakeDepartmentTypeRepository(), new FakeUnitOfWork())
            .Handle(new CreateDepartmentCommand("HR", "الموارد البشرية", TypeCode), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.TypeNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenTypeIsInactive_ReturnsConflict()
    {
        var result = await CreateHandler(new FakeDepartmentRepository(), ActiveTypes(active: false), new FakeUnitOfWork())
            .Handle(new CreateDepartmentCommand("HR", "الموارد البشرية", TypeCode), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.TypeInactive.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenParentIsUnknown_ReturnsValidationError()
    {
        var result = await CreateHandler(new FakeDepartmentRepository(), ActiveTypes(), new FakeUnitOfWork())
            .Handle(
                new CreateDepartmentCommand("HR", "الموارد البشرية", TypeCode, ParentDepartmentId: Guid.NewGuid()),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.ParentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenParentIsInactive_ReturnsConflict()
    {
        var departments = new FakeDepartmentRepository();
        var parent = Department.Create("ROOT", "الإدارة العليا", TypeCode);
        Assert.True(parent.IsSuccess);
        parent.Value.Deactivate(hasActiveChildren: false);
        departments.Add(parent.Value);

        var result = await CreateHandler(departments, ActiveTypes(), new FakeUnitOfWork()).Handle(
            new CreateDepartmentCommand("HR", "الموارد البشرية", TypeCode, ParentDepartmentId: parent.Value.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.InactiveParent.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
