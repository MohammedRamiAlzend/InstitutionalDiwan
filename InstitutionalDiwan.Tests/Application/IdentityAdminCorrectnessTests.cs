using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class IdentityAdminCorrectnessTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : IClock { public DateTime UtcNow => Now; }

    [Theory]
    [InlineData(-10, -2, 0, 5, true)]
    [InlineData(10, 20, 0, 5, true)]
    [InlineData(0, 5, 5, 10, false)] // Inclusive shared endpoint overlaps.
    [InlineData(0, 10, 3, 4, false)]
    [InlineData(3, 4, 0, 10, false)]
    [InlineData(0, 10, 9, 20, false)]
    public void DepartmentAssignments_RejectOnlyOverlappingActiveIntervals(
        int oldStart, int oldEnd, int start, int end, bool allowed)
    {
        var user = User.Create("admin", "hash", Guid.NewGuid(), utcNow: Now).Value;
        var departmentId = Guid.NewGuid();
        Assert.True(user.AssignDepartmentScope(departmentId, startDate: Now.AddDays(oldStart),
            endDate: Now.AddDays(oldEnd), utcNow: Now).IsSuccess);
        var result = user.AssignDepartmentScope(departmentId, startDate: Now.AddDays(start),
            endDate: Now.AddDays(end), utcNow: Now);
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed) Assert.Equal(DomainErrors.Identity.DepartmentAlreadyAssigned, result.Error);
    }

    [Fact]
    public void DepartmentAssignments_OpenEndedOverlapAndEndedHistory()
    {
        var user = User.Create("admin", "hash", Guid.NewGuid(), utcNow: Now).Value;
        var department = Guid.NewGuid();
        Assert.True(user.AssignDepartmentScope(department, utcNow: Now).IsSuccess);
        Assert.True(user.AssignDepartmentScope(department, startDate: Now.AddYears(1), utcNow: Now).IsFailure);
        Assert.True(user.EndDepartmentScope(user.DepartmentAssignments.Single().AssignmentId, utcNow: Now).IsSuccess);
        Assert.True(user.AssignDepartmentScope(department, utcNow: Now).IsSuccess);
        Assert.Equal(2, user.DepartmentAssignments.Count);
    }

    [Fact]
    public async Task AdminHistory_IncludesFutureExpiredAndEnded_WhileEffectiveUsesInclusiveDatesAndActiveRoles()
    {
        var user = User.Create("admin", "hash", Guid.NewGuid(), utcNow: Now).Value;
        var boundaryDepartment = Guid.NewGuid();
        Assert.True(user.AssignDepartmentScope(boundaryDepartment, startDate: Now.AddDays(-1), endDate: Now, utcNow: Now).IsSuccess);
        Assert.True(user.AssignDepartmentScope(Guid.NewGuid(), startDate: Now.AddDays(-3), endDate: Now.AddDays(-2), utcNow: Now).IsSuccess);
        Assert.True(user.AssignDepartmentScope(Guid.NewGuid(), startDate: Now.AddDays(2), utcNow: Now).IsSuccess);
        var endedDepartment = Guid.NewGuid();
        Assert.True(user.AssignDepartmentScope(endedDepartment, utcNow: Now).IsSuccess);
        Assert.True(user.EndDepartmentScope(user.DepartmentAssignments.Single(a => a.DepartmentId == endedDepartment).AssignmentId, utcNow: Now).IsSuccess);
        var activeRole = Role.Create("ACTIVE", "دور فعّال").Value;
        activeRole.GrantPermission(Permissions.UserManage);
        var inactiveRole = Role.Create("INACTIVE", "دور موقوف").Value;
        inactiveRole.GrantPermission(Permissions.RoleManage);
        inactiveRole.Deactivate();
        user.AssignRole(activeRole.RoleId);
        user.AssignRole(inactiveRole.RoleId);
        var users = new FakeUserRepository(); users.Add(user);
        var roles = new FakeRoleRepository(); roles.Add(activeRole); roles.Add(inactiveRole);

        var admin = await new GetUserByIdQueryHandler(users, roles).Handle(new GetUserByIdQuery(user.UserId), default);
        Assert.Equal(4, admin.Value.DepartmentAssignments.Count);
        Assert.Equal(2, admin.Value.Roles.Count); // Inactive grants remain administrable.
        var effective = await new GetUserEffectivePermissionsQueryHandler(users, roles, new Clock())
            .Handle(new GetUserEffectivePermissionsQuery(user.UserId), default);
        Assert.Equal(boundaryDepartment, Assert.Single(effective.Value.DepartmentAssignments).DepartmentId);
        Assert.Equal("ACTIVE", Assert.Single(effective.Value.RoleCodes));
        Assert.Equal(Permissions.UserManage, Assert.Single(effective.Value.Permissions));

        foreach (var assignment in user.DepartmentAssignments.Where(a => a.IsActive).ToList())
            Assert.True(user.EndDepartmentScope(assignment.AssignmentId, utcNow: Now).IsSuccess);
        Assert.All(user.DepartmentAssignments, assignment => Assert.False(assignment.IsActive));
        Assert.Equal(4, user.DepartmentAssignments.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(999)]
    public void UnknownScope_IsAValidationError(int scopeId)
    {
        var create = new CreateUserCommandValidator().Validate(
            new CreateUserCommand("valid-user", "Password123!", Guid.NewGuid(), ScopeTypeId: scopeId));
        var update = new UpdateUserCommandValidator().Validate(
            new UpdateUserCommand(Guid.NewGuid(), Guid.NewGuid(), null, scopeId, false));
        Assert.Contains(create.Errors, e => e.ErrorCode == "Identity.UserScopeTypeInvalid");
        Assert.Contains(update.Errors, e => e.ErrorCode == "Identity.UserScopeTypeInvalid");
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(999, false)]
    public async Task MissingInactiveOrUnsupportedScope_HandlersRejectBeforeSaving(int scopeId, bool inactive)
    {
        var lookups = new FakeLookupRepository();
        if (inactive || scopeId == 999)
        {
            var scope = UserScopeType.Create(scopeId, "Scope", "نطاق").Value;
            if (inactive) scope.Deactivate();
            lookups.Add("user-scope-types", scope);
        }
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "القسم", DepartmentType.SectionId).Value;
        departments.Add(department);
        var user = User.Create("existing", "hash", department.DepartmentId).Value;
        var users = new FakeUserRepository(); users.Add(user);
        var unitOfWork = new FakeUnitOfWork();
        var create = await new CreateUserCommandHandler(users, departments, new FakeEmployeeRepository(),
            new FakeRoleRepository(), new FakePasswordHasher(), unitOfWork, lookups).Handle(
            new CreateUserCommand("new-user", "Password123!", department.DepartmentId, ScopeTypeId: scopeId), default);
        var update = await new UpdateUserCommandHandler(users, departments, new FakeEmployeeRepository(),
            new FakeRoleRepository(), unitOfWork, lookups).Handle(
            new UpdateUserCommand(user.UserId, department.DepartmentId, null, scopeId, false), default);

        Assert.Equal("Identity.UserScopeTypeInvalid", create.Error.Code);
        Assert.Equal(ErrorType.Validation, create.Error.Type);
        Assert.Equal("Identity.UserScopeTypeInvalid", update.Error.Code);
        Assert.Equal(ErrorType.Validation, update.Error.Type);
        Assert.Single(users.Users);
        Assert.Equal(1, user.ScopeTypeId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
