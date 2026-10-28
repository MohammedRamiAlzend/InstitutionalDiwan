using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Projects a <see cref="User"/> into a <see cref="UserResponse"/>, resolving role ids to role codes
/// through <see cref="IRoleRepository"/>. Roles that no longer exist are skipped, so a stale grant
/// never breaks the projection.
/// </summary>
internal static class UserResponseFactory
{
    public static async Task<UserResponse> CreateAsync(
        User user,
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var roles = new List<string>(user.UserRoles.Count);

        foreach (var userRole in user.UserRoles)
        {
            var role = await roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (role is not null)
                roles.Add(role.Code);
        }

        roles.Sort(StringComparer.Ordinal);

        return new UserResponse(
            user.UserId,
            user.Username,
            user.EmployeeId,
            user.PrimaryDepartmentId,
            user.ScopeTypeId,
            user.IsActive,
            user.MustChangePassword,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            roles,
            user.DepartmentAssignments
                .OrderByDescending(assignment => assignment.StartDate)
                .ThenBy(assignment => assignment.AssignmentId)
                .Select(UserDepartmentAssignmentResponse.From)
                .ToList());
    }
}
