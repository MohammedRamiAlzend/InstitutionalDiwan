using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_UpdatesDetailsAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        roles.Add(role.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateRoleCommandHandler(roles, unitOfWork).Handle(
            new UpdateRoleCommand(role.Value.RoleId, "مدير عام", "General Manager", "Updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("مدير عام", role.Value.NameAr);
        Assert.Equal("General Manager", role.Value.NameEn);
        Assert.Equal("Updated", role.Value.Description);
        Assert.Equal("MANAGER", result.Value.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new UpdateRoleCommandHandler(new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new UpdateRoleCommand(Guid.NewGuid(), "مدير"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WithBlankNameAr_ReturnsValidationError()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        roles.Add(role.Value);

        var result = await new UpdateRoleCommandHandler(roles, new FakeUnitOfWork()).Handle(
            new UpdateRoleCommand(role.Value.RoleId, " "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNameEmpty.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
