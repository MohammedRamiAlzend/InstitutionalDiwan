using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the <c>SUPER_ADMIN</c> role (every permission in <see cref="Permissions.All"/>) and the
/// <c>superadmin</c> account assigned to it (Order 305). Runs in <see cref="SeedingModes.Standard"/>
/// and <see cref="SeedingModes.None"/>, so a database always has one account that can administer
/// everything. The account has system scope and belongs to the Central Diwan seeded by
/// <see cref="InstitutionSettingSeeder"/>.
///
/// The role's grants follow <see cref="Permissions.All"/>, so a permission added later reaches the
/// role on the next startup. An existing account is never changed.
/// </summary>
public sealed class SuperAdminSeeder : ISeeder
{
    public const string Username = "superadmin";
    public const string RoleCode = "SUPER_ADMIN";

    public int Order => 305;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public IReadOnlyCollection<string> Modes { get; } = new[] { SeedingModes.Standard, SeedingModes.None };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var role = await EnsureRoleAsync(db, cancellationToken);

        if (await db.Users.AnyAsync(user => user.Username == Username, cancellationToken))
            return;

        var centralDiwanId = await db.Departments
            .Where(department => department.DepartmentTypeId == DepartmentType.CentralDiwanId)
            .Select(department => (Guid?)department.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Central Diwan department not found — InstitutionSettingSeeder (Order 95) must run before SuperAdminSeeder.");

        var created = User.Create(
            Username,
            context.PasswordHasher.Hash(context.Options.DefaultPassword),
            primaryDepartmentId: centralDiwanId,
            scopeTypeId: UserScopeType.SystemId,
            mustChangePassword: false);

        if (created.IsFailure)
            throw new InvalidOperationException(
                $"Invalid user seed data '{Username}': {created.Error.Code} - {created.Error.Description}");

        var assigned = created.Value.AssignRole(role.RoleId);
        if (assigned.IsFailure)
            throw new InvalidOperationException(
                $"Could not assign role '{RoleCode}' to '{Username}': {assigned.Error.Code} - {assigned.Error.Description}");

        db.Users.Add(created.Value);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Role> EnsureRoleAsync(DiwanDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode, cancellationToken);
        if (role is null)
        {
            var created = Role.Create(
                RoleCode,
                "مدير النظام الأعلى",
                "Super Administrator",
                "Every permission in the system",
                isSystemRole: true);

            if (created.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid role seed data '{RoleCode}': {created.Error.Code} - {created.Error.Description}");

            role = created.Value;
            db.Roles.Add(role);
        }

        var granted = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.RoleId)
                .Select(rp => rp.PermissionCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in Permissions.All.Where(code => !granted.Contains(code)))
            role.GrantPermission(code);

        await db.SaveChangesAsync(cancellationToken);
        return role;
    }
}
