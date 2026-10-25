using InstitutionalDiwan.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InstitutionalDiwan.Infrastructure.Authentication;

/// <summary>
/// Ambient caller identity projected from the current request's token. Reads the claims
/// <see cref="JwtProvider"/> issues: <c>sub</c>, <c>dept_id</c>, <c>scope_type</c>, and one
/// <c>permission</c> claim per granted code.
/// </summary>
public sealed class UserContext : IUserContext
{
    private const string DepartmentClaimType = "dept_id";
    private const string ScopeTypeClaimType = "scope_type";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        TryParseGuid(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        ?? TryParseGuid(Principal?.FindFirst("sub")?.Value)
        ?? Guid.Empty;

    public string Username => Principal?.Identity?.Name ?? string.Empty;

    public IReadOnlySet<string> Roles
    {
        get
        {
            var principal = Principal;
            if (principal is null)
                return new HashSet<string>(StringComparer.Ordinal);

            return principal
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);
        }
    }

    public Guid PrimaryDepartmentId =>
        TryParseGuid(Principal?.FindFirst(DepartmentClaimType)?.Value) ?? Guid.Empty;

    public int ScopeTypeId =>
        int.TryParse(Principal?.FindFirst(ScopeTypeClaimType)?.Value, out var scopeTypeId)
            ? scopeTypeId
            : 0;

    public IReadOnlySet<string> Permissions
    {
        get
        {
            var principal = Principal;
            if (principal is null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return principal
                .FindAll(JwtProvider.PermissionClaimType)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;
}
