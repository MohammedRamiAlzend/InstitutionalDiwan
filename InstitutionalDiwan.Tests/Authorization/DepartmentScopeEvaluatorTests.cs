using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Authorization;

public sealed class DepartmentScopeEvaluatorTests
{
    private static Department CreateDepartment(string code, Guid? parentDepartmentId = null)
    {
        var department = Department.Create(code, $"قسم {code}", DepartmentType.SectionId, parentDepartmentId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    private static User CreateUser(
        Guid primaryDepartmentId,
        int scopeTypeId = UserScopeType.DepartmentId)
    {
        var user = User.Create(
            $"{Guid.NewGuid():N}"[..8], "hash", primaryDepartmentId, scopeTypeId: scopeTypeId);

        Assert.True(user.IsSuccess);
        return user.Value;
    }

    private static (DepartmentScopeEvaluator Evaluator,
        FakeUserContext Context,
        FakeUserRepository Users,
        FakeDepartmentRepository Departments) CreateEvaluator()
    {
        var context = new FakeUserContext();
        var users = new FakeUserRepository();
        var departments = new FakeDepartmentRepository();

        return (new DepartmentScopeEvaluator(context, users, departments, TestClock.Instance), context, users, departments);
    }

    [Fact]
    public async Task CanAccessDepartment_WhenAnonymous_ReturnsFalse()
    {
        var (evaluator, context, _, _) = CreateEvaluator();
        context.IsAuthenticated = false;

        Assert.False(await evaluator.CanAccessDepartmentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CanAccessDepartment_ForPrimaryDepartment_ReturnsTrue()
    {
        var (evaluator, context, users, _) = CreateEvaluator();
        var department = CreateDepartment("HR");

        context.IsAuthenticated = true;
        context.UserId = Guid.NewGuid();
        context.PrimaryDepartmentId = department.DepartmentId;
        users.Add(CreateUser(department.DepartmentId, context.ScopeTypeId));

        Assert.True(await evaluator.CanAccessDepartmentAsync(department.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_ForInstitutionScope_ReturnsTrue()
    {
        var (evaluator, context, users, _) = CreateEvaluator();
        var home = CreateDepartment("HOME");
        var other = CreateDepartment("OTHER");

        var user = CreateUser(home.DepartmentId, scopeTypeId: UserScopeType.InstitutionId);

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = home.DepartmentId;
        context.ScopeTypeId = UserScopeType.InstitutionId;
        users.Add(user);

        Assert.True(await evaluator.CanAccessDepartmentAsync(other.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_ForDepartmentTreeScope_IncludesDescendants()
    {
        var (evaluator, context, users, departments) = CreateEvaluator();
        var root = CreateDepartment("ROOT");
        var child = CreateDepartment("CHILD", root.DepartmentId);
        var grandChild = CreateDepartment("GRAND", child.DepartmentId);

        departments.Add(root);
        departments.Add(child);
        departments.Add(grandChild);

        var user = CreateUser(root.DepartmentId, scopeTypeId: UserScopeType.DepartmentTreeId);

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = root.DepartmentId;
        context.ScopeTypeId = UserScopeType.DepartmentTreeId;
        users.Add(user);

        Assert.True(await evaluator.CanAccessDepartmentAsync(grandChild.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_ForActiveAssignmentWithSubtree_ReturnsTrue()
    {
        var (evaluator, context, users, departments) = CreateEvaluator();
        var home = CreateDepartment("HOME");
        var assigned = CreateDepartment("ASSIGNED");
        var child = CreateDepartment("CHILD", assigned.DepartmentId);

        departments.Add(home);
        departments.Add(assigned);
        departments.Add(child);

        var user = CreateUser(home.DepartmentId);
        user.AssignDepartmentScope(assigned.DepartmentId, includeSubtree: true, startDate: TestClock.Instance.UtcNow.AddDays(-1));

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = home.DepartmentId;
        users.Add(user);

        Assert.True(await evaluator.CanAccessDepartmentAsync(child.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_ForAssignmentWithoutSubtree_DoesNotCoverDescendants()
    {
        var (evaluator, context, users, departments) = CreateEvaluator();
        var home = CreateDepartment("HOME");
        var assigned = CreateDepartment("ASSIGNED");
        var child = CreateDepartment("CHILD", assigned.DepartmentId);

        departments.Add(home);
        departments.Add(assigned);
        departments.Add(child);

        var user = CreateUser(home.DepartmentId);
        user.AssignDepartmentScope(assigned.DepartmentId, includeSubtree: false, startDate: TestClock.Instance.UtcNow.AddDays(-1));

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = home.DepartmentId;
        users.Add(user);

        Assert.True(await evaluator.CanAccessDepartmentAsync(assigned.DepartmentId));
        Assert.False(await evaluator.CanAccessDepartmentAsync(child.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_WhenAssignmentHasEnded_ReturnsFalse()
    {
        var (evaluator, context, users, departments) = CreateEvaluator();
        var home = CreateDepartment("HOME");
        var assigned = CreateDepartment("ASSIGNED");

        departments.Add(home);
        departments.Add(assigned);

        var user = CreateUser(home.DepartmentId);
        user.AssignDepartmentScope(assigned.DepartmentId);
        const string assignmentIdLookupError = "expected one assignment";
        Assert.True(user.DepartmentAssignments.Count == 1, assignmentIdLookupError);
        user.EndDepartmentScope(user.DepartmentAssignments.Single().AssignmentId);

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = home.DepartmentId;
        users.Add(user);

        Assert.False(await evaluator.CanAccessDepartmentAsync(assigned.DepartmentId));
    }

    [Fact]
    public async Task CanAccessDepartment_WhenUserIsInactive_ReturnsFalse()
    {
        var (evaluator, context, users, departments) = CreateEvaluator();
        var home = CreateDepartment("HOME");
        var assigned = CreateDepartment("ASSIGNED");

        departments.Add(home);
        departments.Add(assigned);

        var user = CreateUser(home.DepartmentId);
        user.AssignDepartmentScope(assigned.DepartmentId);
        user.Deactivate();

        context.IsAuthenticated = true;
        context.UserId = user.UserId;
        context.PrimaryDepartmentId = Guid.NewGuid();
        users.Add(user);

        Assert.False(await evaluator.CanAccessDepartmentAsync(assigned.DepartmentId));
    }
}
