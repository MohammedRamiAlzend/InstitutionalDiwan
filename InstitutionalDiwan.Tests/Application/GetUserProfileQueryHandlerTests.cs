using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GetUserProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenAuthenticated_ReturnsProfileWithOrderedRolesAndPermissions()
    {
        var userContext = new FakeUserContext
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

        var result = await new GetUserProfileQueryHandler(userContext)
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userContext.UserId, result.Value.UserId);
        Assert.Equal("admin", result.Value.Username);
        Assert.Equal(new[] { "ADMIN", "USER" }, result.Value.Roles);
        Assert.Equal(new[] { Permissions.DocumentCreate, Permissions.DocumentView }, result.Value.Permissions);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var result = await new GetUserProfileQueryHandler(new FakeUserContext())
            .Handle(new GetUserProfileQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.NotAuthenticated.Code, result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }
}
