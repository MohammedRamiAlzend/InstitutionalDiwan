using InstitutionalDiwan.Application.Abstractions.Drive;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InstitutionalDiwan.Infrastructure.OnlyOffice;

/// <summary>
/// HS256 JWT signing/verification for OnlyOffice (Module 11). The token body is exactly the payload
/// JSON (nested structures preserved), which is what OnlyOffice expects when it reads the editor
/// config token and when it signs the save callback. Only the shared secret is used; no issuer or
/// audience validation applies to the OnlyOffice exchange.
/// </summary>
public sealed class OnlyOfficeTokenService : IDriveOnlyOfficeTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly DriveOnlyOfficeOptions _options;

    public OnlyOfficeTokenService(IOptions<DriveOnlyOfficeOptions> options) => _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.JwtSecret);

    public string Sign(IReadOnlyDictionary<string, object?> payload)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OnlyOffice JWT secret is not configured (Drive:OnlyOffice:JwtSecret).");

        var now = DateTimeOffset.UtcNow;
        var body = new Dictionary<string, object?>(payload)
        {
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(_options.TokenLifetimeMinutes <= 0 ? 60 : _options.TokenLifetimeMinutes).ToUnixTimeSeconds()
        };

        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        var payloadPart = Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions));
        var signingInput = $"{header}.{payloadPart}";
        var signature = Base64UrlEncoder.Encode(Sign(Encoding.UTF8.GetBytes(signingInput)));
        return $"{signingInput}.{signature}";
    }

    public IReadOnlyDictionary<string, object?>? Verify(string? token)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        byte[] expected;
        byte[] provided;
        try
        {
            expected = Sign(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"));
            provided = Base64UrlEncoder.DecodeBytes(parts[2]);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(expected, provided))
            return null;

        Dictionary<string, object?>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(Base64UrlEncoder.DecodeBytes(parts[1]), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload is null)
            return null;

        if (payload.TryGetValue("exp", out var expValue) && expValue is JsonElement { ValueKind: JsonValueKind.Number } exp &&
            exp.TryGetInt64(out var expSeconds) &&
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expSeconds)
            return null;

        return payload;
    }

    private byte[] Sign(byte[] input)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.JwtSecret));
        return hmac.ComputeHash(input);
    }
}