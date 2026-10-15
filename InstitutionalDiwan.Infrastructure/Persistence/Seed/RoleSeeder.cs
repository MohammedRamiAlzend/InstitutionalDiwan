using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the system roles (ADMIN, USER) and their permission grants (Order 200, every
/// environment). Runs after <see cref="PermissionSeeder"/> so every granted code exists in the
/// catalog, and before <see cref="UserSeeder"/> which assigns these roles to accounts.
/// </summary>
public sealed class RoleSeeder : ISeeder
{
    public int Order => 200;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    private static readonly string[] AdminPermissions =
    {
        "DOCUMENT_VIEW", "DOCUMENT_CREATE", "DOCUMENT_EDIT", "DOCUMENT_DELETE", "DOCUMENT_APPROVE",
        "DEPARTMENT_MANAGE", "ROLE_MANAGE", "USER_MANAGE", "REPORT_VIEW",
    };

    private static readonly string[] UserPermissions =
    {
        "DOCUMENT_VIEW", "DOCUMENT_CREATE", "DOCUMENT_EDIT",
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        await EnsureRoleAsync(
            db, "ADMIN", "مسؤول إداري", "Administrator", "Full system access",
            isSystemRole: true, AdminPermissions, cancellationToken);

        await EnsureRoleAsync(
            db, "USER", "مستخدم", "User", "Standard user access",
            isSystemRole: false, UserPermissions, cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        DiwanDbContext db,
        string code,
        string nameAr,
        string nameEn,
        string description,
        bool isSystemRole,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
        if (role is null)
        {
            var roleResult = Role.Create(code, nameAr, nameEn, description, isSystemRole);
            if (roleResult.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid role seed data '{code}': {roleResult.Error.Code} - {roleResult.Error.Description}");

            role = roleResult.Value;
            db.Roles.Add(role);
        }

        // Idempotent: only grant permissions the role does not already hold.
        var grantedCodes = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.RoleId)
                .Select(rp => rp.PermissionCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permissionCode in permissionCodes)
        {
            if (!grantedCodes.Contains(permissionCode))
                role.GrantPermission(permissionCode);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
