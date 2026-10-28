using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class GetUserEffectivePermissionsQueryHandler
    : IQueryHandler<GetUserEffectivePermissionsQuery, UserEffectivePermissionsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IClock _clock;

    public GetUserEffectivePermissionsQueryHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IClock clock)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _clock = clock;
    }

    public async Task<Result<UserEffectivePermissionsResponse>> Handle(
        GetUserEffectivePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserEffectivePermissionsResponse>(DomainErrors.Identity.UserNotFound);

        // Same resolution as IdentityTokenIssuer, so this response equals the token's claims.
        var roles = new List<Role>();
        foreach (var userRole in user.UserRoles)
        {
            var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (user.IsActive && role is { IsActive: true })
                roles.Add(role);
        }

        var roleCodes = roles
            .Select(role => role.Code)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        var permissions = roles
            .SelectMany(role => role.RolePermissions)
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var utcNow = _clock.UtcNow;
        var assignments = user.DepartmentAssignments
            .Where(assignment => IsCurrentlyActive(assignment, utcNow))
            .OrderBy(assignment => assignment.DepartmentId)
            .Select(UserDepartmentAssignmentResponse.From)
            .ToList();

        return new UserEffectivePermissionsResponse(
            user.UserId,
            user.Username,
            user.ScopeTypeId,
            user.PrimaryDepartmentId,
            roleCodes,
            permissions,
            assignments);
    }

    private static bool IsCurrentlyActive(UserDepartmentAssignment assignment, DateTime utcNow) =>
        assignment.IsActive &&
        assignment.StartDate <= utcNow &&
        (assignment.EndDate is null || assignment.EndDate.Value >= utcNow);
}
