using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeJwtProvider : IJwtProvider
{
    public AccessToken GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
        => new("fake-access-token", DateTime.UtcNow.AddHours(1));
}
