using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserWithRoles()
    {
        var role = Role.Create("ADMIN", "مسؤول إداري", "Administrator", isSystemRole: true);
        Assert.True(role.IsSuccess);

        var user = User.Create("admin", "hash", Guid.NewGuid());
        Assert.True(user.IsSuccess);
        Assert.True(user.Value.AssignRole(role.Value.RoleId).IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        var roles = new FakeRoleRepository();
        roles.Add(role.Value);

        var result = await new GetUserByIdQueryHandler(users, roles).Handle(
            new GetUserByIdQuery(user.Value.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("admin", result.Value.Username);
        Assert.Equal(new[] { "ADMIN" }, result.Value.Roles);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        var result = await new GetUserByIdQueryHandler(
            new FakeUserRepository(), new FakeRoleRepository()).Handle(
            new GetUserByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
