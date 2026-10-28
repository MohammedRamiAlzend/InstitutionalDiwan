using InstitutionalDiwan.Domain.Entities.Identity;
using System.Security.Cryptography;
using System.Text;

namespace InstitutionalDiwan.Infrastructure.Authentication;

/// <summary>
/// Binds a token to the current account and effective active role/permission sets without exposing
/// the password hash. Issuance and live bearer validation must use this same canonical encoding.
/// </summary>
public static class JwtSessionStamp
{
    public const string ClaimType = "session_stamp";

    public static string Create(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string signingKey) => Convert.ToBase64String(Compute(user, roles, permissions, signingKey));

    public static bool IsValid(
        string? stamp,
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string signingKey)
    {
        // A SHA-256 MAC is always 32 bytes (44 base64 characters). No legacy-token fallback.
        if (stamp is null || stamp.Length != 44)
            return false;

        byte[] supplied;
        try
        {
            supplied = Convert.FromBase64String(stamp);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = Compute(user, roles, permissions, signingKey);
        return CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    private static byte[] Compute(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string signingKey)
    {
        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true);
        var key = Encoding.UTF8.GetBytes(signingKey);
        try
        {
            // Domain separation and length-prefixed strings prevent ambiguous field boundaries.
            writer.Write("InstitutionalDiwan/session-stamp/v1");
            writer.Write(user.UserId.ToString("D"));
            writer.Write(user.Username);
            writer.Write(user.PasswordHash);
            writer.Write(user.EmployeeId?.ToString("D") ?? string.Empty);
            writer.Write(user.PrimaryDepartmentId.ToString("D"));
            writer.Write(user.ScopeTypeId);
            writer.Write(user.IsActive);
            writer.Write(user.MustChangePassword);
            // USERS stores datetime2(3): SQL Server rounds on save while EF retains the original
            // in-memory ticks. Normalize identically so freshly changed passwords survive reload.
            writer.Write(PersistedTicks(user.CreatedAt));
            writer.Write(user.UpdatedAt is { } updatedAt ? PersistedTicks(updatedAt) : 0L);
            // LastLoginAt is deliberately excluded: concurrent logins remain valid.
            WriteSet(writer, roles);
            WriteSet(writer, permissions);
            writer.Flush();
            return HMACSHA256.HashData(key, payload.GetBuffer().AsSpan(0, (int)payload.Length));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(payload.GetBuffer());
        }
    }

    private static long PersistedTicks(DateTime value) =>
        (value.Ticks + TimeSpan.TicksPerMillisecond / 2) / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;

    private static void WriteSet(BinaryWriter writer, IEnumerable<string> values)
    {
        var ordered = values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        writer.Write(ordered.Length);
        foreach (var value in ordered)
            writer.Write(value);
    }
}
