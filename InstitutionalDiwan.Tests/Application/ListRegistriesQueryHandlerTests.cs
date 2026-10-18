using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListRegistriesQueryHandlerTests
{
    private static NumberingRegistry CreateRegistry(string code, string nameAr = "سجل")
    {
        var registry = NumberingRegistry.Create(code, nameAr);
        Assert.True(registry.IsSuccess);
        return registry.Value;
    }

    [Fact]
    public async Task Handle_WithNoRegistries_ReturnsEmptyCollection()
    {
        var handler = new ListRegistriesQueryHandler(new FakeNumberingRegistryRepository());

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

        var handler = new ListRegistriesQueryHandler(registries);
        var result = await handler.Handle(new ListRegistriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, registry => registry.RegistryId == first.RegistryId && registry.Code == "OUTGOING");
        Assert.Contains(result.Value, registry => registry.RegistryId == second.RegistryId && registry.Code == "INCOMING");
    }
}
