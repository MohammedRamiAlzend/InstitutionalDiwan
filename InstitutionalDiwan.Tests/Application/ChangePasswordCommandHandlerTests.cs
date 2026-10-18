using InstitutionalDiwan.Application.Features.Identity.ChangePassword;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ChangePasswordCommandHandlerTests
{
    private const string OldPassword = "OldPass123!";
    private const string NewPassword = "NewPass456!";

    private static (User User, Role Role) CreateUserWithRole(bool isActive = true, bool mustChangePassword = true)
    {
        var user = User.Create(
            "holder",
            new FakePasswordHasher().Hash(OldPassword),
            Guid.NewGuid(),
            scopeTypeId: UserScopeType.SystemId,
            mustChangePassword: mustChangePassword);
        Assert.True(user.IsSuccess);

        var role = Role.Create("USER", "مستخدم", "User");
        Assert.True(role.IsSuccess);
        Assert.True(user.Value.AssignRole(role.Value.RoleId).IsSuccess);

        if (!isActive)
            Assert.True(user.Value.Deactivate().IsSuccess);

        return (user.Value, role.Value);
    }

    private static ChangePasswordCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeRoleRepository roles,
        FakeUnitOfWork unitOfWork)
        => new(users, roles, new FakePasswordHasher(), new FakeJwtProvider(), unitOfWork);

    [Fact]
    public async Task Handle_WithCorrectOldPassword_UpdatesHashClearsFlagAndIssuesToken()
    {
        var (user, role) = CreateUserWithRole(mustChangePassword: true);
        var users = new FakeUserRepository();
        users.Add(user);
        var roles = new FakeRoleRepository();
        roles.Add(role);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(users, roles, unitOfWork).Handle(
            new ChangePasswordCommand(user.UserId, OldPassword, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-access-token", result.Value.AccessToken);
        Assert.Equal(new[] { "USER" }, result.Value.Roles);
        Assert.Equal("holder", result.Value.Username);

        Assert.Equal(new FakePasswordHasher().Hash(NewPassword), user.PasswordHash);
        Assert.False(user.MustChangePassword);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithWrongOldPassword_ReturnsValidationError()
    {
        var (user, role) = CreateUserWithRole();
        var users = new FakeUserRepository();
        users.Add(user);
        var roles = new FakeRoleRepository();
        roles.Add(role);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(users, roles, unitOfWork).Handle(
            new ChangePasswordCommand(user.UserId, "not-my-password", NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.CurrentPasswordIncorrect.Code, result.Error.Code);
        Assert.Equal(new FakePasswordHasher().Hash(OldPassword), user.PasswordHash);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsMissing_ReturnsNotFound()
    {
        var roles = new FakeRoleRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(new FakeUserRepository(), roles, unitOfWork).Handle(
            new ChangePasswordCommand(Guid.NewGuid(), OldPassword, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsForbidden()
    {
        var (user, role) = CreateUserWithRole(isActive: false);
        var users = new FakeUserRepository();
        users.Add(user);
        var roles = new FakeRoleRepository();
        roles.Add(role);

        var result = await CreateHandler(users, roles, new FakeUnitOfWork()).Handle(
            new ChangePasswordCommand(user.UserId, OldPassword, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserInactive.Code, result.Error.Code);
    }
}
