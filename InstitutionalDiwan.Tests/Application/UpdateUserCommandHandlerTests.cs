using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UpdateUserCommandHandlerTests
{
    private static (FakeUserRepository Users, FakeDepartmentRepository Departments, User User) Setup()
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "الموارد البشرية", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        departments.Add(department.Value);

        var user = User.Create("manager", "hash", department.Value.DepartmentId);
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);

        return (users, departments, user.Value);
    }

    [Fact]
    public async Task Handle_WithValidRequest_UpdatesProfileAndSaves()
    {
        var (users, departments, user) = Setup();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateUserCommandHandler(
            users, departments, new FakeEmployeeRepository(), new FakeRoleRepository(), unitOfWork).Handle(
            new UpdateUserCommand(user.UserId, user.PrimaryDepartmentId, EmployeeId: null,
                ScopeTypeId: UserScopeType.InstitutionId, MustChangePassword: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserScopeType.InstitutionId, result.Value.ScopeTypeId);
        Assert.True(result.Value.MustChangePassword);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        var result = await new UpdateUserCommandHandler(
            new FakeUserRepository(), new FakeDepartmentRepository(),
            new FakeEmployeeRepository(), new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new UpdateUserCommand(Guid.NewGuid(), Guid.NewGuid(), null, UserScopeType.DepartmentId, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenDepartmentMissing_ReturnsNotFound()
    {
        var (users, _, user) = Setup();

        var result = await new UpdateUserCommandHandler(
            users, new FakeDepartmentRepository(),
            new FakeEmployeeRepository(), new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new UpdateUserCommand(user.UserId, Guid.NewGuid(), null, UserScopeType.DepartmentId, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
    }
}
