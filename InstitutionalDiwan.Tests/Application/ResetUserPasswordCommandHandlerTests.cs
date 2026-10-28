using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ResetUserPasswordCommandHandlerTests
{
    private const string OriginalPassword = "Original1!";
    private const string TemporaryPassword = "TempPass123!";

    private static (FakeUserRepository Users, User User) Setup(bool mustChangePassword = false)
    {
        var user = User.Create(
            "forgetful",
            new FakePasswordHasher().Hash(OriginalPassword),
            Guid.NewGuid(),
            mustChangePassword: mustChangePassword);
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);

        return (users, user.Value);
    }

    private static ResetUserPasswordCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeUnitOfWork unitOfWork)
        => new(users, new FakeRoleRepository(), new FakePasswordHasher(), unitOfWork);

    [Fact]
    public async Task Handle_WithExistingUser_ReplacesHashAndForcesPasswordChange()
    {
        var (users, user) = Setup(mustChangePassword: false);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(users, unitOfWork).Handle(
            new ResetUserPasswordCommand(user.UserId, TemporaryPassword, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new FakePasswordHasher().Hash(TemporaryPassword), user.PasswordHash);
        Assert.False(new FakePasswordHasher().Verify(OriginalPassword, user.PasswordHash));
        Assert.True(user.MustChangePassword);
        Assert.True(result.Value.MustChangePassword);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(new FakeUserRepository(), unitOfWork).Handle(
            new ResetUserPasswordCommand(Guid.NewGuid(), TemporaryPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
