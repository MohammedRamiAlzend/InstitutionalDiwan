using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandHandlerTests
{
    private static (User User, FakeUserRepository Users, FakeDocumentRepository Documents, FakeUnitOfWork Uow)
        Setup(bool active = true)
    {
        var user = User.Create("author", "hash", Guid.NewGuid(), mustChangePassword: false);
        Assert.True(user.IsSuccess);
        if (!active)
            Assert.True(user.Value.Deactivate().IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        return (user.Value, users, new FakeDocumentRepository(), new FakeUnitOfWork());
    }

    private static CreateDocumentCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeDocumentRepository documents,
        FakeUnitOfWork unitOfWork)
        => new(users, documents, unitOfWork);

    [Fact]
    public async Task Handle_CreatesDraftOwnedByTheCaller()
    {
        var (user, users, documents, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, unitOfWork).Handle(
            new CreateDocumentCommand(
                user.UserId, "طلب تجديد", "LETTER", "INTERNAL", SubjectSummary: "تجديد رخصة"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Document.StatusDraft, result.Value.Status);
        Assert.Equal(user.PrimaryDepartmentId, result.Value.OwningDepartmentId);
        Assert.Equal(user.UserId, result.Value.CreatedByUserId);
        Assert.Equal("LETTER", result.Value.DocumentTypeCode);
        Assert.Equal("INTERNAL", result.Value.Direction);

        var stored = Assert.Single(documents.Documents);
        Assert.Equal(result.Value.DocumentId, stored.DocumentId);
        Assert.Single(stored.StatusHistory);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithInvalidDirection_ReturnsValidationError()
    {
        var (user, users, documents, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, unitOfWork).Handle(
            new CreateDocumentCommand(user.UserId, "Title", "LETTER", "sideways"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DirectionInvalid.Code, result.Error.Code);
        Assert.Empty(documents.Documents);
    }

    [Fact]
    public async Task Handle_WithWhitespaceTitle_ReturnsValidationError()
    {
        var (user, users, documents, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, unitOfWork).Handle(
            new CreateDocumentCommand(user.UserId, "   ", "LETTER", "INTERNAL"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.TitleEmpty.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserIsMissing_ReturnsNotFound()
    {
        var (_, users, documents, unitOfWork) = Setup();
        users.Users.Clear();

        var result = await CreateHandler(users, documents, unitOfWork).Handle(
            new CreateDocumentCommand(Guid.NewGuid(), "Title", "LETTER", "INTERNAL"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsForbidden()
    {
        var (user, users, documents, unitOfWork) = Setup(active: false);

        var result = await CreateHandler(users, documents, unitOfWork).Handle(
            new CreateDocumentCommand(user.UserId, "Title", "LETTER", "INTERNAL"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserInactive.Code, result.Error.Code);
    }
}
