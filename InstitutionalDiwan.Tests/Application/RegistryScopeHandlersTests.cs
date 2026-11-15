using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class RegistryScopeHandlersTests
{
    private sealed class World
    {
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeDocumentNumberRepository DocumentNumbers { get; } = new();
        public FakeLookupRepository Lookups { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public FakeDepartmentScopeEvaluator Scope { get; } = new();
        public FakeDocumentTypeRepository DocumentTypes { get; } = FakeDocumentTypeRepository.WithDefaultTypes();

        public World()
        {
            Lookups.Add("numbering-reset-policies",
                NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
        }

        public Department AddDepartment(string code)
        {
            var department = Department.Create(code, $"قسم {code}", DepartmentType.SectionId).Value;
            Departments.Add(department);
            return department;
        }

        public CreateNumberingRegistryCommandHandler CreateHandler() => new(
            Registries, Departments, DocumentTypes, Lookups, UnitOfWork, Scope);

        public UpdateNumberingRegistryCommandHandler UpdateHandler() => new(
            Registries, DocumentNumbers, Departments, DocumentTypes, Lookups, UnitOfWork, Scope);

        public DeactivateNumberingRegistryCommandHandler DeactivateHandler() => new(
            Registries, UnitOfWork, Scope);

        public ActivateNumberingRegistryCommandHandler ActivateHandler() => new(
            Registries, UnitOfWork, Scope);
    }

    [Fact]
    public async Task Create_WithInstitutionWideScope_AllowsGlobalRegistry()
    {
        var world = new World();
        world.Scope.InstitutionWide = true;

        var result = await world.CreateHandler().Handle(
            new CreateNumberingRegistryCommand("GLOBAL", "عام"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.DepartmentId);
    }

    [Fact]
    public async Task Create_WithinOwnDepartmentScope_Succeeds()
    {
        var world = new World();
        var department = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(department.DepartmentId);

        var result = await world.CreateHandler().Handle(
            new CreateNumberingRegistryCommand("DEPT-BOOK", "سجل القسم", DepartmentId: department.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(department.DepartmentId, result.Value.DepartmentId);
    }

    [Fact]
    public async Task Create_ForAnotherDepartment_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        var other = world.AddDepartment("DEPT-02");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);

        var result = await world.CreateHandler().Handle(
            new CreateNumberingRegistryCommand("DEPT-BOOK", "سجل", DepartmentId: other.DepartmentId),
            CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Create_GlobalRegistry_WithoutInstitutionWideScope_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);

        var result = await world.CreateHandler().Handle(
            new CreateNumberingRegistryCommand("GLOBAL", "عام"), CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Update_KeepingOwnDepartmentScope_Succeeds()
    {
        var world = new World();
        var department = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(department.DepartmentId);
        var registry = NumberingRegistry.Create("DEPT-BOOK", "سجل", departmentId: department.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.UpdateHandler().Handle(
            new UpdateNumberingRegistryCommand(registry.RegistryId, "سجل معدل", DepartmentId: department.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("سجل معدل", result.Value.NameAr);
    }

    [Fact]
    public async Task Update_MovingToAnotherDepartment_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        var other = world.AddDepartment("DEPT-02");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("DEPT-BOOK", "سجل", departmentId: own.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.UpdateHandler().Handle(
            new UpdateNumberingRegistryCommand(registry.RegistryId, "سجل", DepartmentId: other.DepartmentId),
            CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Update_ClearingTheDepartment_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("DEPT-BOOK", "سجل", departmentId: own.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.UpdateHandler().Handle(
            new UpdateNumberingRegistryCommand(registry.RegistryId, "سجل", DepartmentId: null),
            CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Update_AnotherDepartmentsRegistry_IsHiddenAsNotFound()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        var other = world.AddDepartment("DEPT-02");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("OTHER-BOOK", "سجل آخر", departmentId: other.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.UpdateHandler().Handle(
            new UpdateNumberingRegistryCommand(registry.RegistryId, "سجل", DepartmentId: other.DepartmentId),
            CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Update_GlobalRegistry_WithoutInstitutionWideScope_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("GLOBAL", "عام").Value;
        world.Registries.Add(registry);

        var result = await world.UpdateHandler().Handle(
            new UpdateNumberingRegistryCommand(registry.RegistryId, "عام معدل"), CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Deactivate_OwnDepartmentsRegistry_Succeeds()
    {
        var world = new World();
        var department = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(department.DepartmentId);
        var registry = NumberingRegistry.Create("DEPT-BOOK", "سجل", departmentId: department.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.DeactivateHandler().Handle(
            new DeactivateNumberingRegistryCommand(registry.RegistryId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task Deactivate_AnotherDepartmentsRegistry_IsHiddenAsNotFound()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        var other = world.AddDepartment("DEPT-02");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("OTHER-BOOK", "سجل", departmentId: other.DepartmentId).Value;
        world.Registries.Add(registry);

        var result = await world.DeactivateHandler().Handle(
            new DeactivateNumberingRegistryCommand(registry.RegistryId), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Deactivate_GlobalRegistry_WithoutInstitutionWideScope_IsRejected()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("GLOBAL", "عام").Value;
        world.Registries.Add(registry);

        var result = await world.DeactivateHandler().Handle(
            new DeactivateNumberingRegistryCommand(registry.RegistryId), CancellationToken.None);

        AssertForbiddenOutOfScope(result);
    }

    [Fact]
    public async Task Activate_AnotherDepartmentsRegistry_IsHiddenAsNotFound()
    {
        var world = new World();
        var own = world.AddDepartment("DEPT-01");
        var other = world.AddDepartment("DEPT-02");
        world.Scope.AccessibleDepartments.Add(own.DepartmentId);
        var registry = NumberingRegistry.Create("OTHER-BOOK", "سجل", departmentId: other.DepartmentId).Value;
        registry.Deactivate();
        world.Registries.Add(registry);

        var result = await world.ActivateHandler().Handle(
            new ActivateNumberingRegistryCommand(registry.RegistryId), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Activate_WithInstitutionWideScope_Succeeds()
    {
        var world = new World();
        world.Scope.InstitutionWide = true;
        var department = world.AddDepartment("DEPT-01");
        var registry = NumberingRegistry.Create("DEPT-BOOK", "سجل", departmentId: department.DepartmentId).Value;
        registry.Deactivate();
        world.Registries.Add(registry);

        var result = await world.ActivateHandler().Handle(
            new ActivateNumberingRegistryCommand(registry.RegistryId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
    }

    private static void AssertForbiddenOutOfScope(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryOutOfScope.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    private static void AssertNotFound(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
