namespace InstitutionalDiwan.Application.Abstractions.Authentication;

/// <summary>
/// Hashes and verifies user passwords. The concrete algorithm (PBKDF2 / BCrypt / Argon2id) is an
/// infrastructure choice; the stored value must be self-describing (algorithm, work factor, salt).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a self-contained, salted hash for the supplied plain-text password.</summary>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a previously produced hash (constant-time).</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// True when the stored hash uses a work factor below the current one and should be
    /// upgraded on the next successful login.
    /// </summary>
    bool NeedsRehash(string passwordHash);

    /// <summary>
    /// A precomputed hash at the current work factor, so verifying a password for an unknown
    /// username costs the same as for a real account and login timing cannot enumerate users.
    /// </summary>
    string DummyHash { get; }
}
