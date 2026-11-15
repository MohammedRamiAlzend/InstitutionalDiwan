using InstitutionalDiwan.Application.Abstractions.Authentication;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// Deterministic test double — prefix-hash keeps verification simple and inspectable. A
/// <c>legacy:</c> prefix models an old work factor: it still verifies but reports
/// <see cref="NeedsRehash"/> so rehash-on-login can be exercised.
/// </summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"fake:{password}";

    public bool Verify(string password, string passwordHash) =>
        passwordHash == Hash(password) || passwordHash == $"legacy:{password}";

    public bool NeedsRehash(string passwordHash) =>
        passwordHash.StartsWith("legacy:", StringComparison.Ordinal);

    public string DummyHash => "fake:dummy";
}
