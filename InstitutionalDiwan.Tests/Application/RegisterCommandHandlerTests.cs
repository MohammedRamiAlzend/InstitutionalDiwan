using InstitutionalDiwan.Application.Features.Identity.Register;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class RegisterCommandHandlerTests
{
    private static Role CreateUserRole()
    {
        var role = Role.Create("USER", "مستخدم", "User", "Standard user access", isSystemRole: false);
        Assert.True(role.IsSuccess);
        return role.Value;
    }

    private static RegisterCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeRoleRepository roles,
        FakeUnitOfWork unitOfWork)
        => new(users, roles, new FakePasswordHasher(), unitOfWork);

    [Fact]
    public async Task Handle_WithNewUsername_CreatesActiveUserWithUserRole()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        roles.Add(CreateUserRole());
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(users, roles, unitOfWork);

        var result = await handler.Handle(
            new RegisterCommand("  NewUser  ", "Str0ng!Pass"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("newuser", result.Value.Username);
        Assert.Equal(new[] { "USER" }, result.Value.Roles);
        Assert.False(result.Value.MustChangePassword);

        var stored = Assert.Single(users.Users);
        Assert.Equal(result.Value.UserId, stored.UserId);
        Assert.True(stored.IsActive);
        Assert.Single(stored.UserRoles);
        Assert.True(new FakePasswordHasher().Verify("Str0ng!Pass", stored.PasswordHash));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyTaken_ReturnsConflict()
    {
        var users = new FakeUserRepository();
        var existing = User.Create("taken", "existing-hash", Guid.Empty, mustChangePassword: false);
        Assert.True(existing.IsSuccess);
        users.Add(existing.Value);

        var roles = new FakeRoleRepository();
        roles.Add(CreateUserRole());
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(users, roles, unitOfWork);

        var result = await handler.Handle(
            new RegisterCommand("TAKEN", "Whatever123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UsernameTaken.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Single(users.Users);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenDefaultRoleMissing_ReturnsNotFound()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(); // no roles seeded
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(users, roles, unitOfWork);

        var result = await handler.Handle(
            new RegisterCommand("someone", "Whatever123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Empty(users.Users);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ComparesUsernamesCaseInsensitively()
    {
        var users = new FakeUserRepository();
        var existing = User.Create("admin", "hash", Guid.Empty, mustChangePassword: false);
        Assert.True(existing.IsSuccess);
        users.Add(existing.Value);

        var roles = new FakeRoleRepository();
        roles.Add(CreateUserRole());
        var handler = CreateHandler(users, roles, new FakeUnitOfWork());

        var result = await handler.Handle(
            new RegisterCommand("ADMIN", "Whatever123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UsernameTaken.Code, result.Error.Code);
    }
}
