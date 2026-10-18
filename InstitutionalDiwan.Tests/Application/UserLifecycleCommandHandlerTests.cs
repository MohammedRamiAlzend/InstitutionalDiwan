using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UserLifecycleCommandHandlerTests
{
    private static (FakeUserRepository Users, User User) Setup()
    {
        var user = User.Create("manager", "hash", Guid.NewGuid());
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);

        return (users, user.Value);
    }

    [Fact]
    public async Task Deactivate_WithActiveUser_DeactivatesAndSaves()
    {
        var (users, user) = Setup();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new DeactivateUserCommandHandler(users, new FakeRoleRepository(), unitOfWork).Handle(
            new DeactivateUserCommand(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(user.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Deactivate_WhenUserMissing_ReturnsNotFound()
    {
        var result = await new DeactivateUserCommandHandler(
            new FakeUserRepository(), new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new DeactivateUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Activate_WithInactiveUser_ReactivatesAndSaves()
    {
        var (users, user) = Setup();
        Assert.True(user.Deactivate().IsSuccess);

        var result = await new ActivateUserCommandHandler(users, new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new ActivateUserCommand(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Activate_WhenUserMissing_ReturnsNotFound()
    {
        var result = await new ActivateUserCommandHandler(
            new FakeUserRepository(), new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new ActivateUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
    }
}
