using System.Security.Cryptography;
using System.Text;

namespace InstitutionalDiwan.Server.Extensions;

/// <summary>
/// Resolves the JWT signing key during startup. An explicitly configured key always wins (user-secrets
/// in Development, the <c>Jwt__SecretKey</c> environment variable in production). When none is
/// configured, a cryptographically random key is generated once and persisted beside the application,
/// so a restart or a redeployment keeps the same signer instead of silently rotating it.
/// </summary>
public static class JwtSigningKeyExtensions
{
    private const string SecretKeyConfigurationKey = "Jwt:SecretKey";
    private const string SigningKeyPathConfigurationKey = "Jwt:SigningKeyPath";

    /// <summary>Default location, relative to the content root (the publish folder once deployed).</summary>
    private const string DefaultRelativePath = "keys/jwt-signing.key";

    /// <summary>Shortest key accepted by <see cref="AuthenticationExtensions"/> (HMAC-SHA256).</summary>
    private const int MinimumKeyBytes = 32;

    /// <summary>Entropy of a generated key — far above the minimum, base64-encoded in the file.</summary>
    private const int GeneratedKeyBytes = 64;

    /// <summary>
    /// Ensures <c>Jwt:SecretKey</c> is populated before authentication is registered.
    /// </summary>
    /// <returns>Whether a new key was written, and where it lives.</returns>
    public static JwtSigningKeyResult AddInstitutionalJwtSigningKey(this WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration[SecretKeyConfigurationKey]))
            return JwtSigningKeyResult.FromConfiguration;

        var path = ResolvePath(builder);

        string key;
        bool generated;
        try
        {
            (key, generated) = LoadOrCreate(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey is not configured and a signing key could not be written to '{path}'. " +
                "Set Jwt__SecretKey, or point Jwt__SigningKeyPath at a writable folder.", exception);
        }

        // Mutating the configuration here makes the generated key visible to every consumer
        // (AddInstitutionalAuthentication and the session-stamp HMAC) without touching the files.
        builder.Configuration[SecretKeyConfigurationKey] = key;

        return new JwtSigningKeyResult(path, generated);
    }

    private static string ResolvePath(WebApplicationBuilder builder)
    {
        var configuredPath = builder.Configuration[SigningKeyPathConfigurationKey];

        return Path.GetFullPath(
            string.IsNullOrWhiteSpace(configuredPath) ? DefaultRelativePath : configuredPath,
            builder.Environment.ContentRootPath);
    }

    private static (string Key, bool Generated) LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            if (TryReadValid(path, out var existing))
                return (existing, false);

            // Empty or truncated file: no token can be validated with it, so repair it in place.
            return (WriteNewKey(path, overwrite: true), true);
        }

        try
        {
            return (WriteNewKey(path, overwrite: false), true);
        }
        catch (IOException)
        {
            // Another instance starting at the same moment may have created the file first; adopt
            // its key so every instance signs and validates with the same secret.
            if (TryReadValid(path, out var raced))
                return (raced, false);

            throw;
        }
    }

    private static string WriteNewKey(string path, bool overwrite)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(GeneratedKeyBytes));

        using (var stream = new FileStream(
                   path,
                   overwrite ? FileMode.Create : FileMode.CreateNew,
                   FileAccess.Write,
                   FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.Write(key);
        }

        RestrictToCurrentUser(path);
        return key;
    }

    private static bool TryReadValid(string path, out string key)
    {
        key = string.Empty;

        try
        {
            if (!File.Exists(path))
                return false;

            var value = File.ReadAllText(path).Trim();
            if (Encoding.UTF8.GetByteCount(value) < MinimumKeyBytes)
                return false;

            key = value;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void RestrictToCurrentUser(string path)
    {
        // Windows inherits the ACL of the deployment folder; Unix needs an explicit mode because the
        // default umask usually leaves the file group/world readable.
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A read-only volume or a restrictive umask is not fatal: the key itself still works.
        }
    }
}

/// <summary>Outcome of <see cref="JwtSigningKeyExtensions.AddInstitutionalJwtSigningKey"/>.</summary>
/// <param name="KeyPath">File the generated key was read from or written to; <c>null</c> when configured.</param>
/// <param name="Generated">Whether this run wrote a new key.</param>
public readonly record struct JwtSigningKeyResult(string? KeyPath, bool Generated)
{
    public static JwtSigningKeyResult FromConfiguration => new(null, false);
}
