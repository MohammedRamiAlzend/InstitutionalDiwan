using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListRegistriesQueryHandlerTests
{
    private static NumberingRegistry CreateRegistry(
        string code,
        string nameAr = "سجل",
        Guid? departmentId = null)
    {
        var registry = NumberingRegistry.Create(code, nameAr, departmentId: departmentId);
        Assert.True(registry.IsSuccess);
        return registry.Value;
    }

    [Fact]
    public async Task Handle_WithNoRegistries_ReturnsEmptyCollection()
    {
        var handler = new ListRegistriesQueryHandler(
            new FakeNumberingRegistryRepository(), new FakeDepartmentScopeEvaluator());

        var result = await handler.Handle(new ListRegistriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsEveryRegistryWithItsId()
    {
        var registries = new FakeNumberingRegistryRepository();
        var first = CreateRegistry("outgoing", "صادر");
        var second = CreateRegistry("incoming", "وارد");
        registries.Add(first);
        registries.Add(second);

        var handler = new ListRegistriesQueryHandler(
            registries, new FakeDepartmentScopeEvaluator { InstitutionWide = true });
        var result = await handler.Handle(new ListRegistriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, registry => registry.RegistryId == first.RegistryId && registry.Code == "OUTGOING");
        Assert.Contains(result.Value, registry => registry.RegistryId == second.RegistryId && registry.Code == "INCOMING");
    }

    [Fact]
    public async Task Handle_WithDepartmentScope_ReturnsOnlyGlobalAndOwnRegistries()
    {
        var ownDepartmentId = Guid.NewGuid();
        var otherDepartmentId = Guid.NewGuid();
        var registries = new FakeNumberingRegistryRepository();
        var global = CreateRegistry("global", "عام");
        var own = CreateRegistry("own", "خاص", ownDepartmentId);
        var other = CreateRegistry("other", "آخر", otherDepartmentId);
        registries.Add(global);
        registries.Add(own);
        registries.Add(other);

        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(ownDepartmentId);
        var handler = new ListRegistriesQueryHandler(registries, scope);
        var result = await handler.Handle(new ListRegistriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, registry => registry.RegistryId == global.RegistryId);
        Assert.Contains(result.Value, registry => registry.RegistryId == own.RegistryId);
        Assert.DoesNotContain(result.Value, registry => registry.RegistryId == other.RegistryId);
    }

    [Fact]
    public async Task Handle_WithEmptyDepartmentScope_ReturnsOnlyGlobalRegistries()
    {
        var registries = new FakeNumberingRegistryRepository();
        var global = CreateRegistry("global", "عام");
        var departmental = CreateRegistry("departmental", "قسم", Guid.NewGuid());
        registries.Add(global);
        registries.Add(departmental);

        var handler = new ListRegistriesQueryHandler(
            registries, new FakeDepartmentScopeEvaluator());
        var result = await handler.Handle(new ListRegistriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var registry = Assert.Single(result.Value);
        Assert.Equal(global.RegistryId, registry.RegistryId);
    }
}
