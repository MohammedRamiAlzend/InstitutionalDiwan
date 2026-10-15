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
}
