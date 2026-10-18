using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesRoleAndSaves()
    {
        var roles = new FakeRoleRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new CreateRoleCommandHandler(roles, unitOfWork).Handle(
            new CreateRoleCommand("manager", "مدير", "Manager", "Department manager"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MANAGER", result.Value.Code);
        Assert.Equal("مدير", result.Value.NameAr);
        Assert.Equal("Manager", result.Value.NameEn);
        Assert.True(result.Value.IsActive);
        Assert.False(result.Value.IsSystemRole);
        Assert.Empty(result.Value.Permissions);
        Assert.Single(roles.Roles);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsConflict()
    {
        var roles = new FakeRoleRepository();
        var existing = Role.Create("MANAGER", "مدير");
        Assert.True(existing.IsSuccess);
        roles.Add(existing.Value);

        var result = await new CreateRoleCommandHandler(roles, new FakeUnitOfWork()).Handle(
            new CreateRoleCommand("manager", "مدير آخر"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleCodeAlreadyExists.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WithBlankNameAr_ReturnsValidationError()
    {
        var result = await new CreateRoleCommandHandler(new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new CreateRoleCommand("MANAGER", "   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNameEmpty.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
