using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only seeding. Creates initial roles (Admin, User), permissions, and a default admin
/// account (username: admin, password: 12345678) so the login flow can be exercised before real
/// identity provisioning exists.
/// </summary>
public static class NewDiwanDbContextSeeder
{
    public const string DemoUsername = "admin";
    public const string DemoPassword = "12345678";

    private static readonly Guid AdminDepartmentId = Guid.NewGuid();

    public static async Task SeedAsync(
        DiwanDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        // Seed roles if none exist
        if (!await context.Roles.AnyAsync(cancellationToken))
        {
            var adminRoleResult = Role.Create("ADMIN", "مسؤول إداري", "Administrator", "Full system access", isSystemRole: true);
            var userRoleResult = Role.Create("USER", "مستخدم", "User", "Standard user access", isSystemRole: false);

            if (adminRoleResult.IsSuccess && userRoleResult.IsSuccess)
            {
                context.Roles.Add(adminRoleResult.Value);
                context.Roles.Add(userRoleResult.Value);

                var adminRole = adminRoleResult.Value;
                var userRole = userRoleResult.Value;

                adminRole.GrantPermission("DOCUMENT_VIEW");
                adminRole.GrantPermission("DOCUMENT_CREATE");
                adminRole.GrantPermission("DOCUMENT_EDIT");
                adminRole.GrantPermission("DOCUMENT_DELETE");
                adminRole.GrantPermission("DOCUMENT_APPROVE");
                adminRole.GrantPermission("DEPARTMENT_MANAGE");
                adminRole.GrantPermission("ROLE_MANAGE");
                adminRole.GrantPermission("USER_MANAGE");
                adminRole.GrantPermission("REPORT_VIEW");

                userRole.GrantPermission("DOCUMENT_VIEW");
                userRole.GrantPermission("DOCUMENT_CREATE");
                userRole.GrantPermission("DOCUMENT_EDIT");

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        // Seed admin user if none exists
        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN", cancellationToken);
            if (adminRole is not null)
            {
                var userResult = User.Create(
                    DemoUsername,
                    passwordHasher.Hash(DemoPassword),
                    primaryDepartmentId: AdminDepartmentId,
                    scopeType: "SYSTEM",
                    mustChangePassword: false);

                if (userResult.IsSuccess)
                {
                    var user = userResult.Value;
                    user.AssignRole(adminRole.RoleId);
                    context.Users.Add(user);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
