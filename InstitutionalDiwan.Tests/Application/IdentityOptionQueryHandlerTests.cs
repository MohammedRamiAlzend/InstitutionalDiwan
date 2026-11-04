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

    [Fact]
    public async Task UserOptionsByIds_LabelsKnownIdsIncludingInactiveAndSkipsUnknownOnes()
    {
        var users = new FakeUserRepository();
        var active = User.Create("qa.active", "hash", Guid.NewGuid()).Value;
        var inactive = User.Create("qa.inactive", "hash", Guid.NewGuid()).Value;
        inactive.Deactivate();
        users.Add(active); users.Add(inactive);
        users.Add(User.Create("other.user", "hash", Guid.NewGuid()).Value);

        var result = await new GetUserOptionsByIdsQueryHandler(users).Handle(
            new GetUserOptionsByIdsQuery(new[] { inactive.UserId, active.UserId, active.UserId, Guid.NewGuid(), Guid.Empty }),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "qa.active", "qa.inactive" }, result.Value.Select(u => u.Username));
        Assert.False(result.Value.Single(u => u.UserId == inactive.UserId).IsActive);
    }

    [Fact]
    public async Task UserOptionsByIds_ReadsAtMostTheDocumentedNumberOfIds()
    {
        var users = new FakeUserRepository();
        var ids = new List<Guid>();
        for (var i = 0; i < GetUserOptionsByIdsQuery.MaxIds + 5; i++)
        {
            var user = User.Create($"user.{i:D3}", "hash", Guid.NewGuid()).Value;
            users.Add(user);
            ids.Add(user.UserId);
        }

        var result = await new GetUserOptionsByIdsQueryHandler(users)
            .Handle(new GetUserOptionsByIdsQuery(ids), default);

        Assert.Equal(GetUserOptionsByIdsQuery.MaxIds, result.Value.Count);
    }
}
