using InstitutionalDiwan.Application.Features.Identity.AccessGrants;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListDocumentAccessGrantsQueryHandlerTests
{
    private static FakeDocumentAccessGrantRepository GrantsFor(Guid documentId, int count)
    {
        var grants = new FakeDocumentAccessGrantRepository();

        for (var index = 0; index < count; index++)
        {
            var grant = DocumentAccessGrant.Grant(
                documentId, Guid.NewGuid(), AccessLevel.ViewId, Guid.NewGuid());
            Assert.True(grant.IsSuccess);
            grants.Add(grant.Value);
        }

        return grants;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyGrantsForTheDocument()
    {
        var documentId = Guid.NewGuid();
        var grants = GrantsFor(documentId, 2);

        var otherDocument = DocumentAccessGrant.Grant(
            Guid.NewGuid(), Guid.NewGuid(), AccessLevel.ViewId, Guid.NewGuid());
        Assert.True(otherDocument.IsSuccess);
        grants.Add(otherDocument.Value);

        var result = await new ListDocumentAccessGrantsQueryHandler(grants).Handle(
            new ListDocumentAccessGrantsQuery(documentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, grant => Assert.Equal(documentId, grant.DocumentId));
    }

    [Fact]
    public async Task Handle_WithNoGrants_ReturnsEmpty()
    {
        var result = await new ListDocumentAccessGrantsQueryHandler(
            new FakeDocumentAccessGrantRepository()).Handle(
            new ListDocumentAccessGrantsQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
