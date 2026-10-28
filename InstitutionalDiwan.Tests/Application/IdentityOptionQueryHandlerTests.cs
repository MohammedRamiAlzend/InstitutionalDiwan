using InstitutionalDiwan.Application.Features.Identity;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class IdentityOptionQueryHandlerTests
{
    [Fact]
    public async Task UserOptions_SearchMatchesUsernamesAndIncludesInactiveAccounts()
    {
        var users = new FakeUserRepository();
        var active = User.Create("qa.active", "hash", Guid.NewGuid()).Value;
        var inactive = User.Create("qa.inactive", "hash", Guid.NewGuid()).Value;
        inactive.Deactivate();
        users.Add(active); users.Add(inactive);
        users.Add(User.Create("other.user", "hash", Guid.NewGuid()).Value);

        var page = await new ListUserOptionsQueryHandler(users)
            .Handle(new ListUserOptionsQuery(1, 20, "QA."), default);

        Assert.True(page.IsSuccess);
        Assert.Equal(2, page.Value.TotalCount);
        Assert.Equal(new[] { "qa.active", "qa.inactive" }, page.Value.Items.Select(u => u.Username).Order());
        Assert.Equal(active.UserId, page.Value.Items.Single(u => u.Username == "qa.active").UserId);
        Assert.False(page.Value.Items.Single(u => u.Username == "qa.inactive").IsActive);
    }

    [Fact]
    public async Task UserOptions_ArePagedByTheSharedEnvelope()
    {
        var users = new FakeUserRepository();
        foreach (var name in new[] { "user.a", "user.b", "user.c" })
            users.Add(User.Create(name, "hash", Guid.NewGuid()).Value);

        var page = await new ListUserOptionsQueryHandler(users)
            .Handle(new ListUserOptionsQuery(2, 2, "user."), default);

        Assert.Equal(3, page.Value.TotalCount);
        Assert.Equal("user.c", Assert.Single(page.Value.Items).Username);
    }
}
