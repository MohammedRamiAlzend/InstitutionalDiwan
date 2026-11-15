using InstitutionalDiwan.Infrastructure.Authentication;
using System.Security.Cryptography;

namespace InstitutionalDiwan.Tests.Infrastructure;

public sealed class PasswordHasherTests
{
    private const string Password = "Str0ngP@ss!";

    private static string BuildHash(int iterations)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Password, salt, iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    [Fact]
    public void Hash_ThenVerify_RoundTrips()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash(Password);

        Assert.True(hasher.Verify(Password, hash));
        Assert.False(hasher.Verify("wrong", hash));
        Assert.False(hasher.NeedsRehash(hash));
    }

    [Fact]
    public void Verify_AcceptsAHashFromAnOlderWorkFactor()
    {
        var legacy = BuildHash(100_000);

        Assert.True(new PasswordHasher().Verify(Password, legacy));
        Assert.True(new PasswordHasher().NeedsRehash(legacy));
    }

    [Fact]
    public void DummyHash_IsAtTheCurrentWorkFactor()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.NeedsRehash(hasher.DummyHash));
        Assert.True(hasher.Verify("dummy-password-for-login-timing", hasher.DummyHash));
    }
}
