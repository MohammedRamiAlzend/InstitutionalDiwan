using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetRegistryByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRegistryExists_ReturnsMappedResponse()
    {
        var registry = NumberingRegistry.Create("outgoing", "صادر", directionId: DocumentDirection.OutgoingId);
        Assert.True(registry.IsSuccess);
        var registries = new FakeNumberingRegistryRepository();
        registries.Add(registry.Value);

        var handler = new GetRegistryByIdQueryHandler(registries);
        var result = await handler.Handle(
            new GetRegistryByIdQuery(registry.Value.RegistryId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(registry.Value.RegistryId, result.Value.RegistryId);
        Assert.Equal("OUTGOING", result.Value.Code);
        Assert.Equal("صادر", result.Value.NameAr);
        Assert.Equal(DocumentDirection.OutgoingId, result.Value.DirectionId);
        Assert.True(result.Value.IsActive);
        Assert.Empty(result.Value.Counters);
    }

    [Fact]
    public async Task Handle_WhenRegistryIsMissing_ReturnsNotFound()
    {
        var handler = new GetRegistryByIdQueryHandler(new FakeNumberingRegistryRepository());

        var result = await handler.Handle(new GetRegistryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
