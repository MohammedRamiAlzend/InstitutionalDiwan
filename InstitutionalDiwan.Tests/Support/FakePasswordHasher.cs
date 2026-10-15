using InstitutionalDiwan.Application.Abstractions.Authentication;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>Deterministic test double — prefix-hash keeps verification simple and inspectable.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"fake:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}
