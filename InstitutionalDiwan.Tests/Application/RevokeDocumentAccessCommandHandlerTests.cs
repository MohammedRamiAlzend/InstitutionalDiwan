using InstitutionalDiwan.Application.Features.Identity.AccessGrants;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class RevokeDocumentAccessCommandHandlerTests
{
    private static (FakeDocumentAccessGrantRepository Grants, DocumentAccessGrant Grant) Setup()
    {
        var granted = DocumentAccessGrant.Grant(
            Guid.NewGuid(), Guid.NewGuid(), AccessLevel.ViewId, Guid.NewGuid());
        Assert.True(granted.IsSuccess);

        var grants = new FakeDocumentAccessGrantRepository();
        grants.Add(granted.Value);

        return (grants, granted.Value);
    }

    [Fact]
    public async Task Handle_WithActiveGrant_RevokesAndSaves()
    {
        var (grants, grant) = Setup();
        var unitOfWork = new FakeUnitOfWork();
        var revokedBy = Guid.NewGuid();

        var result = await new RevokeDocumentAccessCommandHandler(grants, unitOfWork).Handle(
            new RevokeDocumentAccessCommand(grant.GrantId, revokedBy),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRevoked);
        Assert.Equal(revokedBy, result.Value.RevokedByUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenGrantMissing_ReturnsNotFound()
    {
        var result = await new RevokeDocumentAccessCommandHandler(
            new FakeDocumentAccessGrantRepository(), new FakeUnitOfWork()).Handle(
            new RevokeDocumentAccessCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.AccessGrantNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRevoked_ReturnsConflict()
    {
        var (grants, grant) = Setup();
        Assert.True(grant.Revoke(Guid.NewGuid()).IsSuccess);

        var result = await new RevokeDocumentAccessCommandHandler(grants, new FakeUnitOfWork()).Handle(
            new RevokeDocumentAccessCommand(grant.GrantId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.GrantAlreadyRevoked.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
