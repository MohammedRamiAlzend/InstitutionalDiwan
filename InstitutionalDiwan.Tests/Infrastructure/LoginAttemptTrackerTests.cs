using InstitutionalDiwan.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace InstitutionalDiwan.Tests.Infrastructure;

public sealed class LoginAttemptTrackerTests
{
    [Fact]
    public async Task FiveFailures_LockTheUsername_UntilReset()
    {
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()));

        for (var attempt = 0; attempt < LoginAttemptTracker.MaxFailures - 1; attempt++)
        {
            Assert.False(await tracker.RegisterFailureAsync("user"));
            Assert.False(await tracker.IsLockedOutAsync("user"));
        }

        Assert.True(await tracker.RegisterFailureAsync("user"));
        Assert.True(await tracker.IsLockedOutAsync("user"));
        Assert.False(await tracker.IsLockedOutAsync("other"));

        await tracker.ResetAsync("user");
        Assert.False(await tracker.IsLockedOutAsync("user"));
        Assert.False(await tracker.RegisterFailureAsync("user"));
    }

    [Fact]
    public async Task UsernamesAreCaseInsensitive()
    {
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()));

        for (var attempt = 0; attempt < LoginAttemptTracker.MaxFailures - 1; attempt++)
            await tracker.RegisterFailureAsync("User");

        Assert.True(await tracker.RegisterFailureAsync("user"));
        Assert.True(await tracker.IsLockedOutAsync("USER"));
    }
}
