using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ActivateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_ReactivatesAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        role.Value.Deactivate();
        roles.Add(role.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ActivateRoleCommandHandler(roles, unitOfWork).Handle(
            new ActivateRoleCommand(role.Value.RoleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(role.Value.IsActive);
        Assert.Equal("MANAGER", result.Value.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new ActivateRoleCommandHandler(new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new ActivateRoleCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
