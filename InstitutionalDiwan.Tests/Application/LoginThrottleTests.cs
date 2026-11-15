using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class LoginThrottleTests
{
    private const string Password = "Str0ngP@ss!";

    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeLoginAttemptTracker Tracker { get; } = new();
        public LoginCommandHandler Handler { get; }
        public User Holder { get; }

        public World(
            string username = "holder",
            string? passwordHash = null,
            bool mustChangePassword = false)
        {
            var user = User.Create(
                username,
                passwordHash ?? new FakePasswordHasher().Hash(Password),
                Guid.NewGuid(),
                mustChangePassword: mustChangePassword);
            Assert.True(user.IsSuccess);
            Holder = user.Value;
            Users.Add(user.Value);
            Handler = new LoginCommandHandler(
                Users,
                new FakeRoleRepository(),
                new FakePasswordHasher(),
                new FakeJwtProvider(),
                new FakeUnitOfWork(),
                Tracker);
        }
    }

    [Fact]
    public async Task Login_WithLegacyHash_RehashesSilently()
    {
        var world = new World(passwordHash: "legacy:" + Password);
        var before = world.Holder.UpdatedAt;

        var result = await world.Handler.Handle(new LoginCommand("holder", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"fake:{Password}", world.Holder.PasswordHash);
        Assert.False(world.Holder.MustChangePassword);
        Assert.Equal(before, world.Holder.UpdatedAt);
    }

    [Fact]
    public async Task Login_WithLegacyHashAndForcedChange_KeepsTheOldHash()
    {
        var world = new World(passwordHash: "legacy:" + Password, mustChangePassword: true);

        var result = await world.Handler.Handle(new LoginCommand("holder", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("legacy:" + Password, world.Holder.PasswordHash);
        Assert.True(world.Holder.MustChangePassword);
    }

    [Fact]
    public async Task FourFailures_StillReturnInvalidCredentials()
    {
        var world = new World();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var result = await world.Handler.Handle(
                new LoginCommand("holder", "wrong"), CancellationToken.None);
            AssertInvalidCredentials(result);
        }

        Assert.False(await world.Tracker.IsLockedOutAsync("holder"));
    }

    [Fact]
    public async Task FifthFailure_LocksTheAccountAndReturnsThrottled()
    {
        var world = new World();

        for (var attempt = 0; attempt < 4; attempt++)
            await world.Handler.Handle(new LoginCommand("holder", "wrong"), CancellationToken.None);

        var result = await world.Handler.Handle(new LoginCommand("holder", "wrong"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.LoginThrottled.Code, result.Error.Code);
        Assert.Equal(ErrorType.TooManyRequests, result.Error.Type);
        Assert.True(await world.Tracker.IsLockedOutAsync("holder"));
    }

    [Fact]
    public async Task LockedAccount_RejectsEvenTheCorrectPassword()
    {
        var world = new World();
        for (var attempt = 0; attempt < 5; attempt++)
            await world.Handler.Handle(new LoginCommand("holder", "wrong"), CancellationToken.None);

        var result = await world.Handler.Handle(new LoginCommand("holder", Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.LoginThrottled.Code, result.Error.Code);
        Assert.Equal(ErrorType.TooManyRequests, result.Error.Type);
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsTheFailureCount()
    {
        var world = new World();
        for (var attempt = 0; attempt < 4; attempt++)
            await world.Handler.Handle(new LoginCommand("holder", "wrong"), CancellationToken.None);

        var success = await world.Handler.Handle(new LoginCommand("holder", Password), CancellationToken.None);
        Assert.True(success.IsSuccess);

        var nextFailure = await world.Handler.Handle(new LoginCommand("holder", "wrong"), CancellationToken.None);
        AssertInvalidCredentials(nextFailure);
        Assert.False(await world.Tracker.IsLockedOutAsync("holder"));
    }

    [Fact]
    public async Task UnknownUsernameFailures_CountTowardsTheLockout()
    {
        var world = new World();

        for (var attempt = 0; attempt < 4; attempt++)
            await world.Handler.Handle(new LoginCommand("ghost", "wrong"), CancellationToken.None);

        var result = await world.Handler.Handle(new LoginCommand("ghost", "wrong"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.LoginThrottled.Code, result.Error.Code);
        Assert.Equal(ErrorType.TooManyRequests, result.Error.Type);
    }

    private static void AssertInvalidCredentials(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.InvalidCredentials.Code, result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }
}
