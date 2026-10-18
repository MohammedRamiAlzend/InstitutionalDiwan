using InstitutionalDiwan.Application.Features.Identity.AccessGrants;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GrantDocumentAccessCommandHandlerTests
{
    private static (FakeDocumentAccessGrantRepository Grants, FakeDocumentRepository Documents,
        FakeUserRepository Users, Document Document, User User) Setup()
    {
        var document = Document.CreateDraft(
            "مذكرة", 1, DocumentDirection.InternalId, Guid.NewGuid(), Guid.NewGuid());
        Assert.True(document.IsSuccess);

        var documents = new FakeDocumentRepository();
        documents.Add(document.Value);

        var user = User.Create("grantee", "hash", Guid.NewGuid());
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);

        return (new FakeDocumentAccessGrantRepository(), documents, users, document.Value, user.Value);
    }

    [Fact]
    public async Task Handle_WithValidRequest_GrantsAccessAndSaves()
    {
        var (grants, documents, users, document, user) = Setup();
        var grantedBy = Guid.NewGuid();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new GrantDocumentAccessCommandHandler(
            grants, documents, users, unitOfWork).Handle(
            new GrantDocumentAccessCommand(
                document.DocumentId, user.UserId, AccessLevel.ManageId, grantedBy, "Coverage"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(document.DocumentId, result.Value.DocumentId);
        Assert.Equal(user.UserId, result.Value.UserId);
        Assert.Equal(AccessLevel.ManageId, result.Value.AccessLevelId);
        Assert.False(result.Value.IsRevoked);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(grants.Grants);
    }

    [Fact]
    public async Task Handle_WhenDocumentMissing_ReturnsNotFound()
    {
        var (grants, _, users, _, user) = Setup();

        var result = await new GrantDocumentAccessCommandHandler(
            grants, new FakeDocumentRepository(), users, new FakeUnitOfWork()).Handle(
            new GrantDocumentAccessCommand(Guid.NewGuid(), user.UserId, AccessLevel.ViewId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        var (grants, documents, _, document, _) = Setup();

        var result = await new GrantDocumentAccessCommandHandler(
            grants, documents, new FakeUserRepository(), new FakeUnitOfWork()).Handle(
            new GrantDocumentAccessCommand(document.DocumentId, Guid.NewGuid(), AccessLevel.ViewId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidAccessLevel_ReturnsValidationError()
    {
        var (grants, documents, users, document, user) = Setup();

        var result = await new GrantDocumentAccessCommandHandler(
            grants, documents, users, new FakeUnitOfWork()).Handle(
            new GrantDocumentAccessCommand(document.DocumentId, user.UserId, 99, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.AccessLevelInvalid.Code, result.Error.Code);
    }
}
