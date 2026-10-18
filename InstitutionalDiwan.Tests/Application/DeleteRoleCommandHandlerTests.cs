using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DeleteRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_DeactivatesAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        roles.Add(role.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new DeleteRoleCommandHandler(roles, unitOfWork).Handle(
            new DeleteRoleCommand(role.Value.RoleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(role.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new DeleteRoleCommandHandler(new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new DeleteRoleCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenRoleIsSystemDefined_ReturnsForbidden()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("ADMIN", "مسؤول النظام", isSystemRole: true);
        Assert.True(role.IsSuccess);
        roles.Add(role.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new DeleteRoleCommandHandler(roles, unitOfWork).Handle(
            new DeleteRoleCommand(role.Value.RoleId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.SystemRoleProtected.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.True(role.Value.IsActive);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
