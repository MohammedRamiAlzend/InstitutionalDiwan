using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UpdateDepartmentCommandHandlerTests
{
    private const int TypeId = DepartmentType.SectionId;

    private static Department CreateDepartment(string code, Guid? parentId = null)
    {
        var department = Department.Create(code, "قسم", TypeId, parentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    private static Employee CreateEmployee(Guid departmentId)
    {
        var employee = Employee.Create("أحمد محمد علي", departmentId);
        Assert.True(employee.IsSuccess);
        return employee.Value;
    }

    private static UpdateDepartmentCommandHandler CreateHandler(
        FakeDepartmentRepository departments,
        FakeEmployeeRepository? employees = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeInstitutionSettingRepository? settings = null)
        => new(departments, employees ?? new FakeEmployeeRepository(),
            settings ?? new FakeInstitutionSettingRepository(), unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task Handle_RenamesAndUpdatesExternalPolicy()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(departments, unitOfWork: unitOfWork).Handle(
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
    public async Task Handle_AssignsManagerAndUpdatesNotes()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);
        var employees = new FakeEmployeeRepository();
        var manager = CreateEmployee(department.DepartmentId);
        employees.Employees.Add(manager);

        var result = await CreateHandler(departments, employees).Handle(
            new UpdateDepartmentCommand(
                department.DepartmentId,
                "قسم",
                ParentDepartmentId: null,
                CanSendExternal: false,
                CanReceiveExternal: false,
                ManagerEmployeeId: manager.EmployeeId,
                Notes: "  يُراجع سنويًا  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(manager.EmployeeId, result.Value.ManagerEmployeeId);
        Assert.Equal("يُراجع سنويًا", result.Value.Notes);
    }

    [Fact]
    public async Task Handle_WithNullManagerAndNotes_ClearsBoth()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        department.AssignManager(Guid.NewGuid());
        department.UpdateNotes("ملاحظة قديمة");
        departments.Add(department);

        var result = await CreateHandler(departments).Handle(
            new UpdateDepartmentCommand(
                department.DepartmentId,
                "قسم",
                ParentDepartmentId: null,
                CanSendExternal: false,
                CanReceiveExternal: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ManagerEmployeeId);
        Assert.Null(result.Value.Notes);
    }

    [Fact]
    public async Task Handle_WhenManagerIsUnknown_ReturnsValidationError()
    {
        var departments = new FakeDepartmentRepository();
        var department = CreateDepartment("HR");
        departments.Add(department);

        var result = await CreateHandler(departments).Handle(
            new UpdateDepartmentCommand(
                department.DepartmentId,
                "قسم",
                ParentDepartmentId: null,
                CanSendExternal: false,
                CanReceiveExternal: false,
                ManagerEmployeeId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.ManagerNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Handle_MovesDepartmentUnderNewParent()
    {
        var departments = new FakeDepartmentRepository();
        var root = CreateDepartment("ROOT");
        var department = CreateDepartment("HR");
        departments.Add(root);
        departments.Add(department);

        var result = await CreateHandler(departments).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", root.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(root.DepartmentId, result.Value.ParentDepartmentId);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await CreateHandler(new FakeDepartmentRepository())
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

        var result = await CreateHandler(departments).Handle(
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

        var result = await CreateHandler(departments).Handle(
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

        var result = await CreateHandler(departments).Handle(
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

        var result = await CreateHandler(departments).Handle(
            new UpdateDepartmentCommand(department.DepartmentId, "قسم", parent.DepartmentId, false, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.InactiveParent.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDesignatedCentralDiwanStaysRoot_AllowsNameManagerNotesAndPolicyChanges()
    {
        var departments = new FakeDepartmentRepository();
        var centralDiwan = Department.Create("CENTRAL", "الديوان", DepartmentType.CentralDiwanId).Value;
        departments.Add(centralDiwan);
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(InstitutionSetting.Create(
            "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId,
            centralDiwanDepartmentId: centralDiwan.DepartmentId).Value);
        var employees = new FakeEmployeeRepository();
        var manager = CreateEmployee(centralDiwan.DepartmentId);
        employees.Employees.Add(manager);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(departments, employees, unitOfWork, settings).Handle(
            new UpdateDepartmentCommand(
                centralDiwan.DepartmentId, "الديوان المركزي", null, true, true,
                NameEn: "Central Diwan", ManagerEmployeeId: manager.EmployeeId, Notes: "ملاحظة"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ParentDepartmentId);
        Assert.Equal("الديوان المركزي", result.Value.NameAr);
        Assert.Equal("Central Diwan", result.Value.NameEn);
        Assert.Equal(manager.EmployeeId, result.Value.ManagerEmployeeId);
        Assert.Equal("ملاحظة", result.Value.Notes);
        Assert.True(result.Value.CanSendExternal);
        Assert.True(result.Value.CanReceiveExternal);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(501)]
    public void Validator_EnforcesNotesLimit(int? length)
    {
        var notes = length.HasValue ? new string('n', length.Value) : null;
        var result = new UpdateDepartmentCommandValidator().Validate(
            new UpdateDepartmentCommand(Guid.NewGuid(), "قسم", null, false, false, Notes: notes));

        Assert.Equal(length is null or <= 500, result.IsValid);
        if (!result.IsValid)
        {
            var error = Assert.Single(result.Errors);
            Assert.Equal(nameof(UpdateDepartmentCommand.Notes), error.PropertyName);
            Assert.Equal("MaximumLengthValidator", error.ErrorCode);
        }
    }
}
