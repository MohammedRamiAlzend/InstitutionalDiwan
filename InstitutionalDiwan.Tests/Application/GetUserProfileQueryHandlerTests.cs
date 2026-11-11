using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetUserProfileQueryHandlerTests
{
    private static FakeUserContext AuthenticatedContext() => new()
    {
        IsAuthenticated = true,
        UserId = Guid.NewGuid(),
        Username = "admin",
        Roles = new HashSet<string>(StringComparer.Ordinal) { "USER", "ADMIN" },
        Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.DocumentView,
            Permissions.DocumentCreate
        }
    };

    [Fact]
    public async Task Handle_WhenAuthenticated_ReturnsProfileWithOrderedRolesAndPermissions()
    {
        var userContext = AuthenticatedContext();

        var result = await new GetUserProfileQueryHandler(userContext, new FakeDepartmentScopeEvaluator(), new FakeInstitutionSettingRepository())
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userContext.UserId, result.Value.UserId);
        Assert.Equal("admin", result.Value.Username);
        Assert.Equal(new[] { "ADMIN", "USER" }, result.Value.Roles);
        Assert.Equal(new[] { Permissions.DocumentCreate, Permissions.DocumentView }, result.Value.Permissions);
    }

    [Fact]
    public async Task Handle_WithDepartmentScope_ReturnsTheReachableDepartments()
    {
        var departmentId = Guid.NewGuid();
        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(departmentId);

        var result = await new GetUserProfileQueryHandler(AuthenticatedContext(), scope, new FakeInstitutionSettingRepository())
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { departmentId }, result.Value.AccessibleDepartmentIds);
    }

    [Fact]
    public async Task Handle_WithInstitutionWideScope_ReturnsNullDepartments()
    {
        var scope = new FakeDepartmentScopeEvaluator { InstitutionWide = true };

        var result = await new GetUserProfileQueryHandler(AuthenticatedContext(), scope, new FakeInstitutionSettingRepository())
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AccessibleDepartmentIds);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    public async Task Handle_ReportsWhetherTheCallersDepartmentMayCreateDocuments(
        bool otherDiwansMayCreate, bool callerIsCentralDiwan, bool expected)
    {
        var context = AuthenticatedContext();
        context.PrimaryDepartmentId = Guid.NewGuid();
        var setting = InstitutionalDiwan.Domain.Entities.Organization.InstitutionSetting.Create(
            "المؤسسة", 1, 1,
            centralDiwanDepartmentId: callerIsCentralDiwan ? context.PrimaryDepartmentId : Guid.NewGuid()).Value;
        setting.SetOtherDiwansMayCreateDocuments(otherDiwansMayCreate);
        var settings = new FakeInstitutionSettingRepository();
        settings.Add(setting);

        var result = await new GetUserProfileQueryHandler(context, new FakeDepartmentScopeEvaluator(), settings)
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.CanCreateDocuments);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var result = await new GetUserProfileQueryHandler(new FakeUserContext(), new FakeDepartmentScopeEvaluator(), new FakeInstitutionSettingRepository())
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.NotAuthenticated.Code, result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }
}
