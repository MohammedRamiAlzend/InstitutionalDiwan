using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateUserCommandHandlerTests
{
    private static FakeDepartmentRepository DepartmentsWithOne(out Guid departmentId)
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "الموارد البشرية", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        departments.Add(department.Value);
        departmentId = department.Value.DepartmentId;
        return departments;
    }

    private static CreateUserCommandHandler Handler(
        FakeUserRepository users,
        FakeDepartmentRepository departments,
        FakeRoleRepository roles,
        FakeUnitOfWork unitOfWork,
        FakeEmployeeRepository? employees = null) =>
        new(users, departments, employees ?? new FakeEmployeeRepository(),
            roles, new FakePasswordHasher(), unitOfWork);

    [Fact]
    public async Task Handle_WithValidRequest_CreatesHashedUserAndSaves()
    {
        var users = new FakeUserRepository();
        var departments = DepartmentsWithOne(out var departmentId);
        var unitOfWork = new FakeUnitOfWork();

        var result = await Handler(users, departments, new FakeRoleRepository(), unitOfWork).Handle(
            new CreateUserCommand("  Manager  ", "Secret123", departmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("manager", result.Value.Username);
        Assert.Equal(departmentId, result.Value.PrimaryDepartmentId);
        Assert.Equal(UserScopeType.DepartmentId, result.Value.ScopeTypeId);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = Assert.Single(users.Users);
        Assert.Equal("fake:Secret123", stored.PasswordHash);
    }

    [Fact]
    public async Task Handle_WhenUsernameTaken_ReturnsConflict()
    {
        var users = new FakeUserRepository();
        users.Add(User.Create("manager", "hash", Guid.NewGuid()).Value);
        var departments = DepartmentsWithOne(out var departmentId);

        var result = await Handler(users, departments, new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new CreateUserCommand("MANAGER", "Secret123", departmentId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UsernameTaken.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenDepartmentMissing_ReturnsNotFound()
    {
        var result = await Handler(
            new FakeUserRepository(), new FakeDepartmentRepository(),
            new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new CreateUserCommand("manager", "Secret123", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenEmployeeMissing_ReturnsNotFound()
    {
        var departments = DepartmentsWithOne(out var departmentId);

        var result = await Handler(
            new FakeUserRepository(), departments,
            new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new CreateUserCommand("manager", "Secret123", departmentId, EmployeeId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.EmployeeNotFound.Code, result.Error.Code);
    }
}
