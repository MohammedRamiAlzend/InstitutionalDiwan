using InstitutionalDiwan.Infrastructure.OnlyOffice;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InstitutionalDiwan.Tests.Infrastructure;

public sealed class OnlyOfficeTokenServiceTests
{
    private const string Secret = "unit-test-secret";

    private static OnlyOfficeTokenService Service(string secret = Secret, int lifetimeMinutes = 60) =>
        new(Options.Create(new DriveOnlyOfficeOptions { JwtSecret = secret, TokenLifetimeMinutes = lifetimeMinutes }));

    [Fact]
    public void Sign_ThenVerify_PreservesNestedPayload()
    {
        var service = Service();
        var payload = new Dictionary<string, object?>
        {
            ["document"] = new Dictionary<string, object?> { ["key"] = "k1", ["title"] = "تقرير.docx" },
            ["status"] = 2
        };

        var token = service.Sign(payload);
        var verified = service.Verify(token);

        Assert.NotNull(verified);
        var document = Assert.IsType<JsonElement>(verified!["document"]);
        Assert.Equal("k1", document.GetProperty("key").GetString());
        Assert.Equal("تقرير.docx", document.GetProperty("title").GetString());
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsNull()
    {
        var token = Service().Sign(new Dictionary<string, object?> { ["key"] = "k1" });
        Assert.Null(Service("another-secret").Verify(token));
    }

    [Fact]
    public void Verify_TamperedPayload_ReturnsNull()
    {
        var service = Service();
        var token = service.Sign(new Dictionary<string, object?> { ["key"] = "k1" });
        var parts = token.Split('.');
        var tamperedPayload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"key":"evil"}"""));
        var tampered = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        Assert.Null(service.Verify(tampered));
    }

    [Fact]
    public void Verify_ExpiredToken_ReturnsNull()
    {
        var past = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        var token = BuildToken(Secret, $$"""{"key":"k1","exp":{{past}}}""");
        Assert.Null(Service().Verify(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("only.two")]
    public void Verify_MalformedToken_ReturnsNull(string? token)
    {
        Assert.Null(Service().Verify(token));
    }

    [Fact]
    public void Sign_NotConfigured_Throws()
    {
        var service = Service(string.Empty);
        Assert.False(service.IsConfigured);
        Assert.Throws<InvalidOperationException>(() => service.Sign(new Dictionary<string, object?> { ["key"] = "k1" }));
    }

    [Fact]
    public void Verify_NotConfigured_ReturnsNull()
    {
        var token = Service().Sign(new Dictionary<string, object?> { ["key"] = "k1" });
        Assert.Null(Service(string.Empty).Verify(token));
    }

    private static string BuildToken(string secret, string payloadJson)
    {
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payloadJson));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payload}")));
        return $"{header}.{payload}.{signature}";
    }
}